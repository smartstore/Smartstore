// Native icon elements share metadata and SVG requests; no Vue or jQuery dependency.
(() => {
    'use strict';
    const ns = 'http://www.w3.org/2000/svg';
    const namespace = window.Smartstore = window.Smartstore || {};
    const sizes = new Set(['2xs', 'xs', 'sm', 'lg', 'xl', '2xl', ...Array.from({ length: 10 }, (_, i) => `${i + 1}x`)]);
    const animations = new Set(['spin', 'pulse', 'beat', 'fade', 'throb', 'bounce', 'flip', 'shake', 'spin-snap', 'swing', 'wag']);
    const presentation = ['flip-h', 'flip-v', 'stroke-scale', 'rotate', 'size', 'font-scale', 'fw', 'color', 'inverse', 'animation', 'animation-duration', 'animation-reverse', 'scale', 'shift-x', 'shift-y', 'mirror-rtl'];
    const drawing = ['name', 'area', 'lib', 'variant'];
    const cache = new Map();
    const waiting = [];
    let active = 0, manifestPromise, cloneId = 0;
    // Inert nodes never upgrade into custom elements or start drawing requests.
    const sourceDocument = document.implementation.createHTMLDocument('');

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

    function prepareManifest(data) {
        // Old cached pages fall back to the endpoint while scripts and manifests roll over.
        if (data.schemaVersion !== 12) return null;
        const mirrorInRtl = new Set(data.mirrorInRtl);
        const concepts = new Map();
        for (const [kitName, kit] of Object.entries(data.kits)) {
            const areaMaps = kit.areas.map(area => {
                if (!concepts.has(area)) concepts.set(area, new Map());
                return concepts.get(area);
            });
            const url = new URL(kit.url, rootUrl()).href;
            const duotone = new Set(kit.duotone);
            const strokeScaling = new Set(kit.supportsStrokeScaling);
            for (const name of kit.icons) {
                // The server has resolved these identities already. Fill omitted fields from
                // kit defaults; do not remap names or use patches to choose a sprite/symbol.
                const source = Object.hasOwn(kit.sources, name) ? kit.sources[name] : name;
                const colon = source.indexOf(':'), slash = source.indexOf('/');
                const library = colon < 0 ? kit.defaultLibrary : source.slice(0, colon);
                const variant = slash < 0 ? kit.defaultVariant : source.slice(slash + 1);
                const icon = source.slice(colon + 1, slash < 0 ? source.length : slash);
                const reference = {
                    transform: kit.transforms?.[name],
                    supportsStrokeScaling: strokeScaling.has(name),
                    href: url + '#' + encodeURIComponent(name),
                    address: `${library.toLowerCase()}:${icon}/${variant.toLowerCase()}`,
                    classes: ['icon-' + library, 'icon-' + library + '-' + variant,
                        ...(mirrorInRtl.has(name) ? ['icon-mirror-rtl'] : []),
                        ...(duotone.has(name) ? ['icon-duotone'] : [])]
                };
                for (let i = 0; i < kit.areas.length; i++) {
                    if (data.areas[kit.areas[i]]?.[name] === kitName) areaMaps[i].set(name, reference);
                }
            }
        }
        return concepts;
    }

    // Only plain concepts use the manifest. The server owns address parsing, selections
    // and modifier precedence, including explicit false/zero overrides of mapping defaults.
    // Rendering-only modifiers must not create drawing requests or bypass kit lookup.
    function splitPresentation(name) {
        const separator = name?.indexOf('?') ?? -1;
        const result = { name, color: null, size: null, animation: null, rotate: null, 'stroke-scale': null, 'flip-h': null, 'flip-v': null };
        if (separator < 0) return result;
        const modifiers = [];
        for (const pair of name.slice(separator + 1).split('&')) {
            const equals = pair.indexOf('=');
            const token = equals < 0 ? pair : pair.slice(0, equals);
            const key = token === 'anim' ? 'animation' : token;
            if (key === 'flip') {
                const value = pair.slice(equals + 1);
                if (!['x', 'y', 'xy', 'none'].includes(value)) throw new Error('Invalid icon flip.');
                result['flip-h'] = String(value.includes('x'));
                result['flip-v'] = String(value.includes('y'));
                continue;
            }
            if (!['color', 'size', 'animation', 'rotate', 'stroke-scale', 'flip-h', 'flip-v'].includes(key)) { modifiers.push(pair); continue; }
            const raw = equals < 0 ? (key.startsWith('flip-') ? 'true' : '') : pair.slice(equals + 1);
            const value = key === 'color' ? decodeURIComponent(raw) : raw;
            if (result[key] !== null || !value.trim()
                || (key === 'color' && /[;{}]/.test(value))
                || (['rotate', 'stroke-scale'].includes(key) && !Number.isFinite(Number(value)))
                || (key === 'stroke-scale' && Number(value) <= 0)
                || (key === 'size' && !sizes.has(value))
                || (key === 'animation' && !animations.has(value))) {
                throw new Error('Invalid or duplicate icon ' + key + '.');
            }
            result[key] = value;
        }
        result.name = name.slice(0, separator) + (modifiers.length ? '?' + modifiers.join('&') : '');
        return result;
    }

    function isConcept(input) {
        return !/[:\/@!?]/.test(input.name) && drawing.every((key, i) => i === 0 || key === 'area' || input[key] == null);
    }

    function manifest() {
        if (!manifestPromise) {
            const path = document.querySelector('meta[property="sm:icons"]')?.content;
            if (!path) return Promise.resolve(null);
            manifestPromise = request(new URL(path, rootUrl()).href, true).then(prepareManifest).catch(() => {
                // A missing historical manifest must not prevent server-side resolution.
                manifestPromise = null;
                return null;
            });
        }
        return manifestPromise;
    }

    // Missing, false and true are distinct: false must be able to reset mapping defaults.
    function boolean(value) {
        if (value == null) return undefined;
        if (value === '' || value === '1' || value.toLowerCase() === 'true') return true;
        if (value === '0' || value.toLowerCase() === 'false') return false;
        throw new Error('Expected true or false for icon attribute.');
    }

    function number(value, integer = false) {
        if (value == null) return undefined;
        if (!value.trim() || !Number.isFinite(Number(value)) || (integer && (!Number.isInteger(Number(value)) || Number(value) < -2147483648 || Number(value) > 2147483647))) throw new Error('Invalid numeric icon attribute.');
        return Number(value);
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
        const manifestPath = document.querySelector('meta[property="sm:icons"]')?.content || 'icons/manifest/';
        const renderUrl = new URL('../render?' + key, new URL(manifestPath, rootUrl()));
        const promise = request(renderUrl.href).then(svgTemplate);
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
        // Presentation belongs to the host, not to both the host and its drawing.
        for (const name of [...svg.classList]) if (name === 'icon' || name.startsWith('icon-')) svg.classList.remove(name);
        svg.removeAttribute('role'); svg.removeAttribute('aria-label'); svg.removeAttribute('aria-labelledby');
        svg.setAttribute('aria-hidden', 'true'); svg.setAttribute('focusable', 'false');
        return svg;
    }

    class IconElementBase extends HTMLElement {
        constructor() {
            super();
            this._internals = this.attachInternals();
            this._identity = [];
            this._sequence = 0;
            this._generatedClasses = new Set();
            this._generatedStyles = new Map();
            this._sourceObserver = new MutationObserver(records => this.captureAttributes(records));
        }
        ensureSource() {
            if (this._source) return;
            this._source = sourceDocument.createElement(this.localName);
            for (const attribute of this.attributes) this._source.setAttribute(attribute.name, attribute.value);
            this.observeSource();
        }
        observeSource() {
            this._sourceObserver.observe(this, { attributes: true });
        }
        captureAttributes(records) {
            for (const name of new Set(records.map(record => record.attributeName))) {
                if (name === 'class') {
                    const authored = new Set(this._source.classList);
                    const classes = [...this.classList].filter(value => !this._generatedClasses.has(value) || authored.has(value));
                    if (this.hasAttribute(name)) this._source.setAttribute(name, classes.join(' '));
                    else this._source.removeAttribute(name);
                } else if (name === 'style') {
                    const style = sourceDocument.createElement('span').style;
                    style.cssText = this.getAttribute(name) || '';
                    for (const [key, value] of this._generatedStyles) {
                        if (style.getPropertyValue(key) === value) style.removeProperty(key);
                    }
                    if (this.hasAttribute(name)) this._source.setAttribute(name, style.cssText);
                    else this._source.removeAttribute(name);
                } else if (this.hasAttribute(name)) this._source.setAttribute(name, this.getAttribute(name));
                else this._source.removeAttribute(name);
            }
        }
        render(action) {
            this.ensureSource();
            // Drain external changes before disconnecting: same-turn edits must be saved too.
            this.captureAttributes(this._sourceObserver.takeRecords());
            this._sourceObserver.disconnect();
            this._rendering = true;
            try { action(); }
            finally { this._rendering = false; this.observeSource(); }
        }
        /** Returns an inert copy of authored markup, without generated classes, styles or SVGs. */
        getSourceNode() {
            this.ensureSource();
            this.captureAttributes(this._sourceObserver.takeRecords());
            const node = this._source.cloneNode(false);
            if (this.localName === 'sm-icon-stack') {
                for (const child of this.childNodes) {
                    node.append(child.getSourceNode ? child.getSourceNode() : sourceDocument.importNode(child, true));
                }
            }
            return node;
        }
        static get observedAttributes() {
            return [...presentation, ...drawing, 'class', 'style', 'aria-label', 'aria-labelledby', 'aria-hidden', 'role'];
        }
        connectedCallback() { this.ensureSource(); this.schedule(); }
        disconnectedCallback() { this._sequence++; }
        attributeChangedCallback(name, oldValue, newValue) {
            if (this._rendering || oldValue === newValue) return;
            if (presentation.includes(name) && this.isConnected) {
                // These attributes change presentation only; preserve the drawing and pending load.
                try { this.present(this.localName === 'sm-icon-stack'); }
                catch (error) { this.dispatchEvent(new CustomEvent('icon-error', { detail: { error }, bubbles: true })); }
            } else this.schedule();
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
            const mirrorRtl = boolean(this.getAttribute('mirror-rtl'));
            if (mirrorRtl !== undefined) {
                const index = classes.indexOf('icon-mirror-rtl');
                if (index >= 0) classes.splice(index, 1);
                if (mirrorRtl) classes.push('icon-mirror-rtl');
            }
            const styles = new Map();
            const size = this.getAttribute('size') ?? this._addressPresentation?.size;
            const animation = this.getAttribute('animation') ?? this._addressPresentation?.animation;
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
                const value = this.getAttribute(attribute) ?? (attribute === 'color' ? this._addressPresentation?.color : null);
                if (value != null) { if (/[;{}]/.test(value)) throw new Error('Invalid CSS value.'); styles.set(css, value); }
            }
            const reverse = boolean(this.getAttribute('animation-reverse'));
            if (reverse !== undefined) styles.set('--icon-animation-direction', reverse ? 'reverse' : 'normal');
            const strokeScale = number(this.getAttribute('stroke-scale') ?? this._addressPresentation?.['stroke-scale']);
            if (strokeScale !== undefined) {
                if (strokeScale <= 0) throw new Error('Stroke scale must be positive.');
                if (!stack) styles.set('--icon-stroke-scale', String(strokeScale));
            }
            const rotate = number(this.getAttribute('rotate') ?? this._addressPresentation?.rotate);
            const mapping = stack ? [0, 0, 0] : (this._mapping || [0, 0, 0]);
            const flips = ['flip-h', 'flip-v'].map((attr, i) => {
                const value = boolean(this.getAttribute(attr) ?? this._addressPresentation?.[attr]);
                const changed = value !== undefined && value !== !!mapping[i];
                if (value !== undefined) classes.push('icon-flip-' + (i ? 'v' : 'h') + (changed ? '' : '-reset'));
                return changed;
            });
            if (flips[0] && flips[1]) {
                classes.splice(classes.indexOf('icon-flip-h'), 1);
                classes.splice(classes.indexOf('icon-flip-v'), 1);
                classes.push('icon-flip-hv');
            }
            if (rotate !== undefined || flips[0] !== flips[1]) {
                const angle = (rotate ?? mapping[2]) - (flips[0] !== flips[1] ? -mapping[2] : mapping[2]);
                const normalized = (angle % 360 + 360) % 360;
                if ([90, 180, 270].includes(normalized)) classes.push('icon-rotate-' + normalized);
                else styles.set('--icon-rotate', angle + 'deg');
            }
            this.render(() => {
                for (const name of this._generatedClasses) {
                    if (!this._source.classList.contains(name)) this.classList.remove(name);
                }
                this._generatedClasses = new Set(classes);
                this.classList.add(...classes);
                for (const key of this._generatedStyles.keys()) {
                    const authored = this._source.style.getPropertyValue(key);
                    if (authored) this.style.setProperty(key, authored, this._source.style.getPropertyPriority(key));
                    else this.style.removeProperty(key);
                }
                this._generatedStyles.clear();
                for (const [key, value] of styles) {
                    const internalKey = key.replace('--icon-', '--_icon-');
                    if (this._source.style.getPropertyValue(internalKey)) continue;
                    this.style.setProperty(internalKey, value);
                    this._generatedStyles.set(internalKey, this.style.getPropertyValue(internalKey));
                }
                if (!this.style.length && !this._source.hasAttribute('style')) this.removeAttribute('style');
            });
            const labelled = this.hasAttribute('aria-label') || this.hasAttribute('aria-labelledby');
            // Explicit author ARIA attributes take precedence over these defaults.
            this._internals.role = labelled ? 'img' : null;
            this._internals.ariaHidden = !stack && !labelled ? 'true' : null;
        }
    }

    class IconElement extends IconElementBase {
        async update() {
            const sequence = this._sequence;
            const input = Object.fromEntries(drawing.map(key => [key, this.getAttribute(key)]));
            const parsed = splitPresentation(input.name);
            input.name = parsed.name;
            this._addressPresentation = parsed;
            this.present(this.localName === 'sm-icon-stack');
            input.area = input.area || document.querySelector('meta[property="sm:icons"]')?.dataset.area || 'public';
            const key = JSON.stringify(input);
            if (this._drawingKey === key) return;
            // Do not show the old icon under a newly requested name while loading.
            this.replaceChildren(); this._drawingKey = null; this._identity = [];
            this.present(this.localName === 'sm-icon-stack');
            if (!input.name) return;
            const reference = isConcept(input) ? (await manifest())?.get(input.area)?.get(input.name) : null;
            let template;
            if (reference) {
                template = document.createElementNS(ns, 'svg');
                template.setAttribute('data-icon', input.name + ' -> ' + reference.address);
                if (reference.transform) template.setAttribute('data-icon-mapping', reference.transform);
                if (reference.supportsStrokeScaling) template.setAttribute('data-supports-stroke-scaling', 'true');
                template.setAttribute('class', reference.classes.join(' '));
                const use = document.createElementNS(ns, 'use');
                use.setAttribute('href', reference.href); template.append(use);
            } else template = await loadDrawing(input);
            if (!this.isConnected || this._sequence !== sequence) return;
            // Manifest hits and endpoint responses carry the same server-resolved identity.
            // The source snapshot excludes the generated presentation on the host.
            this._mapping = (template.getAttribute('data-icon-mapping') || '0 0 0').split(' ').map(Number);
            this._identity = [...template.classList].filter(name => name.startsWith('icon-'));
            this.present(this.localName === 'sm-icon-stack');
            this.replaceChildren(cloneDrawing(template));
            this._drawingKey = key;
            if (template.classList.contains('icon-not-found')) {
                throw new Error('Icon not found: ' + input.name);
            }
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
        animations: Object.freeze([...animations]),
        // Explicit registration preserves existing Vue compiler rules and other components.
        install(app) {
            const previous = app.config.compilerOptions.isCustomElement;
            app.config.compilerOptions.isCustomElement = tag => tag === 'sm-icon' || tag === 'sm-icon-stack' || !!previous?.(tag);
        }
    };
    customElements.define('sm-icon', IconElement);
    customElements.define('sm-icon-stack', IconStackElement);
})();
