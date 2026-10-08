// Native icon elements share metadata and SVG requests; no Vue or jQuery dependency.
(() => {
    'use strict';
    const ns = 'http://www.w3.org/2000/svg';
    const namespace = window.Smartstore = window.Smartstore || {};
    const sizes = new Set(['2xs', 'xs', 'sm', 'lg', 'xl', '2xl', ...Array.from({ length: 10 }, (_, i) => `${i + 1}x`)]);
    const animations = new Set(['spin', 'pulse', 'spin-pulse', 'beat', 'fade', 'throb', 'cylon', 'cylon-vertical', 'bounce', 'beat-fade', 'flip', 'shake']);
    const presentation = ['size', 'font-scale', 'fw', 'color', 'inverse', 'animation', 'animation-duration', 'animation-reverse', 'scale', 'shift-x', 'shift-y'];
    const drawing = ['name', 'lib', 'variant', 'rotate', 'flip-h', 'flip-v', 'stroke-scale'];
    const cache = new Map();
    const waiting = [];
    let active = 0, manifestPromise, cloneId = 0;

    function own(object, key) {
        return object && Object.prototype.hasOwnProperty.call(object, key) ? object[key] : undefined;
    }

    function rootUrl() {
        return new URL(document.querySelector('meta[property="sm:root"]')?.content || './', document.baseURI);
    }

    // Bound simultaneous network requests; every queued request still shares its promise.
    function request(url, json = false) {
        return new Promise((resolve, reject) => {
            waiting.push(async () => {
                try {
                    const response = await fetch(url, { credentials: 'same-origin' });
                    if (!response.ok) throw new Error(`Icon request failed (${response.status}).`);
                    resolve(await (json ? response.json() : response.text()));
                } catch (error) { reject(error); }
                finally { active--; pump(); }
            });
            pump();
        });
    }

    function pump() {
        while (active < 6 && waiting.length) {
            active++;
            waiting.shift()();
        }
    }

    function modifiers(value) {
        const result = {};
        if (value === undefined) return result;
        const seen = new Set();
        for (const pair of value.split('&')) {
            const parts = pair.split('=');
            const [key, text] = parts;
            if (parts.length !== 2 || seen.has(key)) throw new Error('Invalid icon modifier.');
            seen.add(key);
            if (key === 'flip' && ['x', 'y', 'xy', 'none'].includes(text)) {
                result.x = text.includes('x'); result.y = text.includes('y');
            } else if (['rotate', 'stroke-scale'].includes(key) && /^[+-]?(?:\d+(?:\.\d*)?|\.\d+)$/.test(text) && Number.isFinite(Number(text))) {
                if (key === 'stroke-scale' && Number(text) <= 0) throw new Error('Invalid stroke multiplier.');
                result[key === 'rotate' ? 'rotation' : 'stroke'] = key === 'rotate' ? ((Number(text) % 360) + 360) % 360 : Number(text);
            } else throw new Error('Unknown or invalid icon modifier.');
        }
        return result;
    }

    function target(value) {
        const index = value.indexOf('?');
        return { name: index < 0 ? value : value.slice(0, index), ...modifiers(index < 0 ? undefined : value.slice(index + 1)) };
    }

    function transformKey(name, value) {
        return JSON.stringify([name, value.x ?? false, value.y ?? false, value.rotation ?? 0]);
    }

    function prepareManifest(data) {
        if (data.schemaVersion !== 2) throw new Error('Unsupported icon manifest.');
        const aliases = new Map(), concepts = new Map(), symbols = new Map();
        const kitNames = Object.keys(data.kits).sort((a, b) => a === b ? 0 : a === 'shared' ? -1 : b === 'shared' ? 1 : a < b ? -1 : 1);
        for (const [name, library] of Object.entries(data.libraries)) {
            library.name = name; library.key = library.shortName || name;
            library.aliases = new Map();
            aliases.set(name.toLowerCase(), library);
            if (library.shortName) aliases.set(library.shortName.toLowerCase(), library);
            for (const [variantName, variant] of Object.entries(library.variants)) {
                variant.name = variantName; variant.key = variant.shortName || variantName;
                library.aliases.set(variantName.toLowerCase(), variant);
                if (variant.shortName) library.aliases.set(variant.shortName.toLowerCase(), variant);
            }
        }
        for (const kitName of kitNames) {
            for (const concept of data.kits[kitName].concepts) if (!concepts.has(concept)) concepts.set(concept, data.kits[kitName]);
        }
        for (const [selection, urls] of Object.entries(data.urls)) {
            const [libKey] = selection.split('@');
            const library = aliases.get(libKey.toLowerCase());
            const index = new Map();
            for (const kitName of kitNames) {
                // Preserve server priority even when the winning kit isn't advertised.
                for (const concept of data.kits[kitName].concepts) {
                    const mapped = target(own(library.mapping, concept) ?? concept);
                    const key = transformKey(mapped.name, mapped);
                    if (!index.has(key)) index.set(key, urls[kitName] ? new URL(urls[kitName], rootUrl()).href + '#' + encodeURIComponent(concept) : null);
                }
            }
            symbols.set(selection, index);
        }
        return { data, aliases, concepts, symbols };
    }

    function manifest() {
        if (!manifestPromise) {
            const path = document.querySelector('meta[property="sm:icons"]')?.content;
            if (!path) return Promise.reject(new Error('Missing sm:icons manifest URL.'));
            manifestPromise = request(new URL(path, rootUrl()).href, true).then(prepareManifest).catch(error => {
                manifestPromise = null;
                throw error;
            });
        }
        return manifestPromise;
    }

    // Missing, false and true are distinct: false must be able to reset mapping defaults.
    function boolean(value) {
        if (value == null) return undefined;
        if (value === '' || value.toLowerCase() === 'true') return true;
        if (value.toLowerCase() === 'false') return false;
        throw new Error('Expected true or false for icon attribute.');
    }

    function number(value, integer = false) {
        if (value == null) return undefined;
        if (!value.trim() || !Number.isFinite(Number(value)) || (integer && (!Number.isInteger(Number(value)) || Number(value) < -2147483648 || Number(value) > 2147483647))) throw new Error('Invalid numeric icon attribute.');
        return Number(value);
    }

    function resolve(index, input) {
        const expression = target(input.name);
        const match = /^(?:([a-zA-Z0-9_-]+):)?([^:@!?]+?)(!)?(?:@([a-zA-Z0-9_-]+))?$/.exec(expression.name);
        if (!match) throw new Error('Invalid icon address.');
        const [, lib, concept, direct, variantName] = match;
        if (!concept.trim() || ['.', '..'].includes(concept) || /[\x00-\x1f\x7f-\x9f/\\]/.test(concept)) throw new Error('Invalid icon name.');
        const kit = direct ? null : index.concepts.get(concept);
        const library = index.aliases.get((lib ?? input.lib ?? kit?.defaultLibrary ?? index.data.defaultLibrary).toLowerCase());
        if (!library) throw new Error('Unknown icon library.');
        if (lib != null && input.lib != null && index.aliases.get(input.lib.toLowerCase()) !== library) throw new Error('Conflicting icon libraries.');
        const kitLibrary = index.aliases.get((kit?.defaultLibrary ?? index.data.defaultLibrary).toLowerCase());
        const selected = variantName ?? input.variant ?? (kitLibrary === library ? kit?.defaultVariant : null)
            ?? (library.name === index.data.defaultLibrary ? index.data.defaultVariant : null) ?? library.defaultVariant;
        const variant = library.aliases.get(selected.toLowerCase());
        if (!variant) throw new Error('Unknown icon variant.');
        if (variantName != null && input.variant != null && library.aliases.get(input.variant.toLowerCase()) !== variant) throw new Error('Conflicting icon variants.');
        const mapped = target(direct ? concept : own(library.mapping, concept) ?? concept);
        const inherited = { x: mapped.x ?? false, y: mapped.y ?? false, rotation: mapped.rotation ?? 0, stroke: mapped.stroke ?? 1 };
        const effective = { ...inherited };
        for (const key of ['x', 'y', 'rotation', 'stroke']) if (expression[key] !== undefined) effective[key] = expression[key];
        const addressChanged = transformKey(mapped.name, effective) !== transformKey(mapped.name, inherited);
        for (const [attribute, key] of [['flip-h', 'x'], ['flip-v', 'y']]) {
            const value = boolean(input[attribute]);
            if (value !== undefined) effective[key] = value;
        }
        const rotate = number(input.rotate, true), stroke = number(input['stroke-scale']);
        if (rotate !== undefined) effective.rotation = ((rotate % 360) + 360) % 360;
        if (stroke !== undefined) effective.stroke = stroke;
        if (effective.stroke <= 0) throw new Error('Stroke scale must be positive.');
        const selection = library.key + '@' + variant.key;
        const href = !addressChanged && effective.stroke === 1 && transformKey(mapped.name, inherited) === transformKey(mapped.name, effective)
            ? index.symbols.get(selection)?.get(transformKey(mapped.name, effective)) : null;
        return { address: `${library.key}:${mapped.name}@${variant.key}`, library: library.key, variant: variant.key, href };
    }

    function svgTemplate(text) {
        const svg = new DOMParser().parseFromString(text, 'image/svg+xml').documentElement;
        if (svg.localName !== 'svg' || svg.namespaceURI !== ns || svg.querySelector('parsererror')) throw new Error('Invalid icon SVG response.');
        return document.importNode(svg, true);
    }

    function loadDrawing(input) {
        // Only drawing options enter this key. Colors, sizes and animations share artwork.
        const query = new URLSearchParams();
        for (const key of drawing) if (input[key] != null) {
            query.set(({ 'flip-h': 'flipH', 'flip-v': 'flipV', 'stroke-scale': 'strokeScale' })[key] || key,
                key.startsWith('flip-') ? String(boolean(input[key])) : input[key]);
        }
        const key = query.toString();
        if (cache.has(key)) {
            const existing = cache.get(key);
            cache.delete(key); cache.set(key, existing);
            return existing;
        }
        const promise = request(new URL('icons/render?' + key, rootUrl()).href).then(svgTemplate);
        cache.set(key, promise);
        promise.then(() => {
            // Evict settled entries only; in-flight requests always remain shared.
            promise.settled = true;
            for (const [oldKey, value] of cache) if (cache.size > 256 && value.settled) cache.delete(oldKey);
        }, () => { if (cache.get(key) === promise) cache.delete(key); });
        return promise;
    }

    function cloneDrawing(template) {
        const svg = template.cloneNode(true);
        const ids = new Map();
        const prefix = 'sm-icon-' + (++cloneId) + '-';
        for (const node of [svg, ...svg.querySelectorAll('[id]')]) if (node.id) {
            ids.set(node.id, prefix + node.id); node.id = prefix + node.id;
        }
        // Inline gradient/mask IDs must not collide between clones of the same template.
        if (ids.size) for (const node of [svg, ...svg.querySelectorAll('*')]) for (const attr of [...node.attributes]) {
            let value = attr.value.replace(/url\(#([^)]*)\)/g, (all, id) => ids.has(id) ? `url(#${ids.get(id)})` : all);
            if (attr.localName === 'href' && value.startsWith('#') && ids.has(value.slice(1))) value = '#' + ids.get(value.slice(1));
            if (value !== attr.value) node.setAttributeNS(attr.namespaceURI, attr.name, value);
        }
        // Presentation belongs to the host; source styles such as stroke scale stay on the SVG.
        svg.classList.remove(...[...svg.classList].filter(name => name === 'icon' || name.startsWith('icon-')));
        svg.removeAttribute('role'); svg.removeAttribute('aria-label'); svg.removeAttribute('aria-labelledby');
        svg.setAttribute('aria-hidden', 'true'); svg.setAttribute('focusable', 'false');
        return svg;
    }

    class IconElementBase extends HTMLElement {
        constructor() {
            super();
            this._generatedClasses = [];
            this._identity = [];
            this._internal = false;
            this._sequence = 0;
        }
        static get observedAttributes() {
            return [...presentation, ...drawing, 'class', 'style', 'aria-label', 'aria-labelledby', 'aria-hidden', 'role'];
        }
        connectedCallback() {
            if (this._authoredClass === undefined) this._authoredClass = this.getAttribute('class') || '';
            if (this._authoredStyle === undefined) this._authoredStyle = this.getAttribute('style') || '';
            this.schedule();
        }
        disconnectedCallback() { this._sequence++; }
        attributeChangedCallback(name, oldValue, newValue) {
            if (this._internal || oldValue === newValue) return;
            if (name === 'role') this._autoRole = false;
            if (name === 'aria-hidden') this._autoHidden = false;
            if (name === 'class') this._authoredClass = (newValue || '').split(/\s+/).filter(x => !this._generatedClasses.includes(x)).join(' ');
            if (name === 'style') {
                // Remove only declarations we previously generated; preserve caller edits.
                const style = document.createElement('span').style;
                style.cssText = newValue || '';
                for (const [key, value] of this._generatedStyles || []) if (style.getPropertyValue(key).trim() === value) style.removeProperty(key);
                this._authoredStyle = style.cssText;
            }
            this.schedule();
        }
        schedule() {
            if (!this.isConnected || this._scheduled) return;
            // Invalidate immediately, before an earlier fetch can commit its result.
            this._sequence++;
            this._scheduled = true;
            queueMicrotask(() => {
                this._scheduled = false;
                const sequence = this._sequence;
                if (this.isConnected) this.update().catch(error => {
                    if (!this.isConnected || this._sequence !== sequence) return;
                    this.dispatchEvent(new CustomEvent('icon-error', { detail: { error }, bubbles: true }));
                });
            });
        }
        present(stack) {
            const classes = [stack ? 'icon-stack' : 'icon', ...this._identity];
            const styles = new Map();
            const size = this.getAttribute('size'), animation = this.getAttribute('animation');
            if (size != null) { if (!sizes.has(size)) throw new Error('Unsupported icon size.'); classes.push('icon-' + size); }
            if (animation != null) { if (!animations.has(animation)) throw new Error('Unsupported icon animation.'); classes.push('icon-' + animation); }
            if (boolean(this.getAttribute('fw'))) classes.push('icon-fw');
            if (boolean(this.getAttribute('inverse'))) classes.push('icon-inverse');
            for (const [attribute, css, factor, unit] of [
                ['font-scale', '--icon-size-factor', 1, ''], ['scale', '--icon-scale', 1, ''],
                ['shift-x', '--icon-shift-x', 1 / 16, 'em'], ['shift-y', '--icon-shift-y', 1 / 16, 'em']]) {
                const value = number(this.getAttribute(attribute));
                if (value !== undefined) styles.set(css, String(value * factor) + unit);
            }
            for (const [attribute, css] of [['color', '--icon-color'], ['animation-duration', '--icon-animation-duration']]) {
                const value = this.getAttribute(attribute);
                if (value != null) { if (/[;{}]/.test(value)) throw new Error('Invalid CSS value.'); styles.set(css, value); }
            }
            const reverse = boolean(this.getAttribute('animation-reverse'));
            if (reverse !== undefined) styles.set('--icon-animation-direction', reverse ? 'reverse' : 'normal');
            if (stack) {
                const rotate = number(this.getAttribute('rotate'), true);
                if (rotate !== undefined) styles.set('--icon-rotate', rotate + 'deg');
                for (const [attr, css] of [['flip-h', '--icon-flip-x'], ['flip-v', '--icon-flip-y']]) {
                    const value = boolean(this.getAttribute(attr));
                    if (value !== undefined) styles.set(css, value ? '-1' : '1');
                }
            }
            this._internal = true;
            try {
                this.setAttribute('class', [...new Set([...classes, ...(this._authoredClass || '').split(/\s+/).filter(Boolean)])].join(' '));
                this.setAttribute('style', [...styles].map(([key, value]) => `${key}:${value};`).join('') + (this._authoredStyle || ''));
                this._generatedClasses = classes; this._generatedStyles = styles;
                const labelled = this.hasAttribute('aria-label') || this.hasAttribute('aria-labelledby');
                if (this._autoHidden) { this.removeAttribute('aria-hidden'); this._autoHidden = false; }
                if (this._autoRole) { this.removeAttribute('role'); this._autoRole = false; }
                if (labelled && !this.hasAttribute('role')) { this.setAttribute('role', 'img'); this._autoRole = true; }
                if (!stack && !labelled && !this.hasAttribute('aria-hidden')) { this.setAttribute('aria-hidden', 'true'); this._autoHidden = true; }
            } finally { this._internal = false; }
        }
    }

    class IconElement extends IconElementBase {
        async update() {
            const sequence = this._sequence;
            this.present(false);
            const input = Object.fromEntries(drawing.map(key => [key, this.getAttribute(key)]));
            const key = JSON.stringify(input);
            if (this._drawingKey === key) return;
            if (!input.name) { this.replaceChildren(); this._drawingKey = null; this.removeAttribute('data-icon'); return; }
            // Do not show the old icon under a newly requested name while loading.
            this.replaceChildren(); this._drawingKey = null; this._identity = []; this.removeAttribute('data-icon');
            const index = await manifest();
            const resolved = resolve(index, input);
            let template;
            if (resolved.href) {
                template = document.createElementNS(ns, 'svg');
                const use = document.createElementNS(ns, 'use');
                use.setAttribute('href', resolved.href); template.append(use);
            } else template = await loadDrawing(input);
            if (!this.isConnected || this._sequence !== sequence) return;
            this._identity = ['icon-' + resolved.library, 'icon-' + resolved.library + '-' + resolved.variant];
            this.present(false);
            this.setAttribute('data-icon', template.getAttribute('data-icon') || resolved.address);
            this.replaceChildren(cloneDrawing(template));
            this._drawingKey = key;
            this.dispatchEvent(new CustomEvent('icon-load', { bubbles: true }));
        }
    }

    class IconStackElement extends IconElementBase {
        connectedCallback() {
            super.connectedCallback();
            this._observer ??= new MutationObserver(() => this.schedule());
            this._observer.observe(this, { childList: true });
        }
        disconnectedCallback() { super.disconnectedCallback(); this._observer?.disconnect(); }
        async update() {
            this.present(true);
            if ([...this.children].some(child => child.localName !== 'sm-icon')) throw new Error('sm-icon-stack accepts direct sm-icon children only.');
        }
    }

    namespace.Icons = {
        // Explicit registration preserves existing Vue compiler rules and other components.
        install(app) {
            const previous = app.config.compilerOptions.isCustomElement;
            app.config.compilerOptions.isCustomElement = tag => tag === 'sm-icon' || tag === 'sm-icon-stack' || !!previous?.(tag);
        }
    };
    customElements.define('sm-icon', IconElement);
    customElements.define('sm-icon-stack', IconStackElement);
})();
