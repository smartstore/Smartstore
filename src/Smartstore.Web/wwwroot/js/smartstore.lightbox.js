// Smartstore Lightbox: image and video viewing with native pointer gestures.
(() => {
    'use strict';

    const namespace = window.Smartstore = window.Smartstore || {};
    if (namespace.Lightbox) return;

    const instances = new WeakMap();
    const clamp = (value, min, max) => Math.max(min, Math.min(max, value));
    const distance = (a, b) => Math.hypot(b.x - a.x, b.y - a.y);
    const midpoint = (a, b) => ({ x: (a.x + b.x) / 2, y: (a.y + b.y) / 2 });
    const nativeControl = target => target.closest('button, a, input, select, textarea, video, [contenteditable]');
    let active = null;

    // Options configure behavior: loop, closeOnScroll and history (false, true or a gallery ID).
    // Integration hooks: index, getThumbnailRect(index), onChange(index), onClose().
    // Presentation, including motion and idle timing, comes from --lb-* properties in CSS.
    class Lightbox {
        static defaults = {
            loop: true,
            closeOnScroll: true,
            history: true
        };

        // Temporary test switch. The original scripts, template and styles stay available.
        static useLegacy = false;

        static getInstance(element) {
            return instances.get(element);
        }

        constructor(element, items, options = {}) {
            this.element = element;
            this.items = items;
            this.options = { ...Lightbox.defaults, ...options };
            this.index = clamp(Number(this.options.index) || 0, 0, Math.max(0, items.length - 1));
            this.pointers = new Map();
            this.timers = new Map();
        }

        open() {
            if (this.isOpen || !this.items.length) return this;
            active?.destroy();
            active = this;
            instances.set(this.element, this);
            this.isOpen = true;
            this.styles = getComputedStyle(this.element);

            if (this.options.legacy || Lightbox.useLegacy) {
                this.legacy = new LegacyLightbox(this);
                this.legacy.open();
                return this;
            }

            this.events = new AbortController();
            this.stage = this.element.querySelector('.lightbox-stage');
            this.track = this.element.querySelector('.lightbox-track');
            this.background = this.element.querySelector('.lightbox-bg');
            this.loader = this.element.querySelector('.lightbox-loader');
            this.loader.hidden = true;
            this.loader.replaceChildren(window.createCircularSpinner(64, true)[0]);
            this.slots = [...this.track.children].map(element => ({ element, index: -1 }));
            this.zoomInIcon = this.element.querySelector('[data-lightbox-icon="zoom-in"]');
            this.zoomOutIcon = this.element.querySelector('[data-lightbox-icon="zoom-out"]');
            this.fullscreenEnterIcon = this.element.querySelector('[data-lightbox-icon="fullscreen-enter"]');
            this.fullscreenExitIcon = this.element.querySelector('[data-lightbox-icon="fullscreen-exit"]');
            this.updateFullscreen();
            this.element.hidden = false;
            this.element.setAttribute('aria-hidden', 'false');
            this.element.classList.toggle('lightbox-touch', !!window.touchable);
            this.element.classList.toggle('lightbox-single', this.items.length === 1);
            this.element.classList.remove('lightbox-idle', 'lightbox-controls-hidden');
            this.element.style.opacity = '1';
            this.background.style.opacity = this.cssNumber('opacity');
            this.scrollY = window.scrollY;
            this.resize();
            this.bindEvents();
            this.openHistory();
            this.notifyChange();
            window.AccessKitFocusTrap?.activate(this.element, { exclude: '.lightbox-slide[aria-hidden="true"] *' });
            this.showControls();
            this.animateOpening();
            return this;
        }

        on(target, type, listener, options = {}) {
            target.addEventListener(type, listener, { ...options, signal: this.events.signal });
        }

        cssNumber(name) {
            return parseFloat(this.styles.getPropertyValue(`--lb-${name}`)) || 0;
        }

        duration(name = 'duration') {
            const value = this.styles.getPropertyValue(`--lb-${name}`).trim();
            return (parseFloat(value) || 0) * (value.endsWith('ms') ? 1 : 1000);
        }

        later(name, callback, delay) {
            clearTimeout(this.timers.get(name));
            this.timers.set(name, setTimeout(() => {
                this.timers.delete(name);
                if (this.isOpen) callback();
            }, delay));
        }

        bindEvents() {
            this.on(this.element, 'click', event => {
                const button = event.target.closest('[data-lightbox-action]');
                if (!button || button.disabled) return;
                event.preventDefault();
                this.showControls();
                switch (button.dataset.lightboxAction) {
                    case 'close': this.close(); break;
                    case 'previous': this.prev(); break;
                    case 'next': this.next(); break;
                    case 'zoom': this.toggleZoom(); break;
                    case 'fullscreen': this.toggleFullscreen(); break;
                }
            });
            this.on(this.element, 'dblclick', event => {
                if (event.target.closest('[data-lightbox-action]')) event.preventDefault();
            });
            this.on(this.stage, 'pointerdown', event => this.pointerDown(event));
            this.on(this.stage, 'pointermove', event => this.pointerMove(event));
            this.on(this.stage, 'pointerup', event => this.pointerUp(event));
            this.on(this.stage, 'pointercancel', event => this.pointerUp(event, true));
            this.on(this.stage, 'lostpointercapture', event => {
                if (this.pointers.has(event.pointerId)) this.pointerUp(event, true);
            });
            this.on(this.element, 'pointermove', event => {
                if (event.pointerType !== 'mouse') return;
                this.element.classList.add('lightbox-mouse');
                this.showControls();
            });
            this.on(this.element, 'mouseleave', () => {
                if (this.duration('idle-outside-delay')) this.later('idle', () => this.element.classList.add('lightbox-idle'), this.duration('idle-outside-delay'));
            });
            this.on(this.element, 'focusin', () => this.showControls());
            this.on(this.element, 'keydown', event => {
                if (event.key === 'Escape') {
                    event.preventDefault();
                    this.close();
                }
                else if (!event.altKey && !event.ctrlKey && !event.metaKey &&
                    !event.target.closest('video, input, textarea, select, [contenteditable]')) {
                    if (event.key === 'ArrowLeft' || event.key === 'ArrowRight') {
                        event.preventDefault();
                        this.synchronizeDirection();
                        const step = (event.key === 'ArrowRight' ? 1 : -1) * this.inlineDirection;
                        step < 0 ? this.prev() : this.next();
                        this.showControls();
                    }
                }
            });
            this.on(this.element, 'wheel', event => this.wheel(event), { passive: false });
            this.on(window, 'resize', () => this.resize());
            if (window.visualViewport) this.on(window.visualViewport, 'resize', () => this.resize());
            this.on(window, 'scroll', () => {
                if (this.options.closeOnScroll && !document.fullscreenElement && !this.pointers.size && Math.abs(window.scrollY - this.scrollY) > 2) this.close();
            }, { passive: true });
            this.on(document, 'fullscreenchange', () => {
                this.updateFullscreen();
                this.resize();
            });
        }

        resize() {
            if (!this.isOpen || this.closing) return;
            this.finishAnimation();
            this.inlineDirection = getComputedStyle(this.element).direction === 'rtl' ? -1 : 1;
            this.element.style.setProperty('--lb-inline-direction', this.inlineDirection);
            this.width = this.stage.clientWidth;
            this.height = this.stage.clientHeight;
            this.step = this.width * (1 + this.cssNumber('spacing'));
            this.resetGesture();
            this.render();
        }

        synchronizeDirection() {
            const direction = getComputedStyle(this.element).direction === 'rtl' ? -1 : 1;
            if (this.inlineDirection !== direction) this.resize();
        }

        // Programmatic navigation wraps even for two items, as PhotoSwipe's goTo did.
        // Gestures only wrap with at least three items, avoiding duplicate holders.
        normalize(index, gesture = false) {
            if (this.options.loop && (!gesture || this.items.length > 2)) return (index % this.items.length + this.items.length) % this.items.length;
            return clamp(index, 0, this.items.length - 1);
        }

        render() {
            this.track.style.transform = 'translate3d(0,0,0)';
            const desired = [-1, 0, 1].map(offset => {
                const index = this.index + offset;
                return this.options.loop && this.items.length > 2 ? this.normalize(index) : index;
            });
            // Reuse the arriving holder (including its loaded image) during navigation.
            const available = [...this.slots];
            this.slots = desired.map(index => {
                let slot = available.find(slot => slot.index === index);
                if (!slot) slot = available.find(slot => !desired.includes(slot.index)) || available[0];
                available.splice(available.indexOf(slot), 1);
                if (slot.index !== index || !slot.content) this.fill(slot, index);
                return slot;
            });
            this.element.querySelector('.lightbox-caption-text').textContent = this.items[this.index].title || '';
            this.element.querySelector('.lightbox-caption').hidden = !this.items[this.index].title;
            this.slots.forEach((slot, position) => {
                const current = position === 1;
                slot.element.style.transform = `translate3d(${(position - 1) * this.step * this.inlineDirection}px,0,0)`;
                slot.element.setAttribute('aria-hidden', String(!current));
                slot.element.inert = !current;
                slot.element.setAttribute('aria-label', `${slot.index + 1} / ${this.items.length}`);
                this.layout(slot);
            });
            this.current = this.slots[1];
            this.zoom = this.current.fit || 1;
            this.pan = { ...this.current.center };
            this.paint();
            this.element.querySelector('.lightbox-counter').textContent = `${this.index + 1}/${this.items.length}`;
            this.updateLoading();
        }

        clearSlot(slot) {
            slot.events?.abort();
            slot.element.querySelectorAll('video').forEach(video => {
                video.pause();
                video.removeAttribute('src');
                video.load();
            });
            slot.element.replaceChildren();
            slot.content = null;
            slot.image = null;
        }

        fill(slot, index) {
            this.clearSlot(slot);
            slot.index = index;
            const item = this.items[index];
            if (!item) return;
            slot.events = new AbortController();
            const content = slot.content = document.createElement('div');
            slot.element.append(content);
            if (item.type === 'video') {
                content.className = 'lightbox-video hstack justify-content-center';
                const video = document.createElement('video');
                video.className = 'lightbox-video-item';
                video.controls = true;
                video.preload = 'metadata';
                video.playsInline = true;
                video.tabIndex = 0;
                video.src = item.src;
                content.append(video);
                return;
            }
            content.className = 'lightbox-zoom';
            const image = slot.image = new Image();
            image.className = 'lightbox-image';
            image.alt = item.alt || '';
            image.draggable = false;
            image.decoding = 'async';
            slot.loaded = false;
            slot.error = false;
            const loaded = () => {
                slot.loaded = true;
                content.querySelector('.lightbox-placeholder')?.remove();
                if (!item.w || !item.h) {
                    item.w = image.naturalWidth;
                    item.h = image.naturalHeight;
                    if (this.isOpen) this.resize();
                }
                if (slot === this.current) this.updateLoading();
            };
            image.addEventListener('load', loaded, { signal: slot.events.signal });
            image.addEventListener('error', () => {
                slot.loaded = true;
                slot.error = true;
                content.replaceChildren();
                const message = document.createElement('div');
                message.className = 'lightbox-error hstack justify-content-center gap-2 px-3';
                const messageText = document.createElement('div');
                messageText.className = 'lightbox-error-text text-break';
                const link = document.createElement('a');
                link.href = item.src;
                link.textContent = this.element.dataset.errorMessage;
                messageText.append(link);
                const errorIcon = document.createElement('sm-icon');
                errorIcon.setAttribute('name', 'alert-warning');
                errorIcon.setAttribute('aria-hidden', 'true');
                errorIcon.className = 'flex-shrink-0';
                message.append(errorIcon, messageText);
                content.append(message);
                content.style.cssText = 'inline-size:100%;block-size:100%';
                if (slot === this.current) {
                    this.updateLoading();
                    this.paint();
                }
            }, { signal: slot.events.signal });
            if (item.msrc) {
                const placeholder = new Image();
                placeholder.className = 'lightbox-placeholder';
                placeholder.alt = '';
                placeholder.src = item.msrc;
                content.append(placeholder);
            }
            content.append(image);
            image.src = item.src;
            if (image.complete && image.naturalWidth) loaded();
        }

        layout(slot) {
            const item = this.items[slot.index];
            if (!item || item.type === 'video') {
                slot.fit = 1;
                slot.center = { x: 0, y: 0 };
                return;
            }
            if (slot.error) {
                slot.content.style.cssText = 'inline-size:100%;block-size:100%';
                slot.fit = 1;
                slot.center = { x: 0, y: 0 };
                return;
            }
            const captionHeight = item.title ? this.element.querySelector('.lightbox-caption').offsetHeight : 0;
            slot.areaHeight = this.height - captionHeight;
            slot.fit = Math.min(1, this.width / (item.w || this.width), slot.areaHeight / (item.h || slot.areaHeight));
            slot.center = { x: Math.round((this.width - item.w * slot.fit) / 2), y: Math.round((slot.areaHeight - item.h * slot.fit) / 2) };
            slot.content.style.inlineSize = `${item.w}px`;
            slot.content.style.blockSize = `${item.h}px`;
            this.transformImage(slot, slot.center, slot.fit);
        }

        transformImage(slot, pan, zoom) {
            // Pan and zoom anchors use physical X/Y. Offset the canvas positioned
            // at inline-start so the same coordinates work with a right-hand origin.
            const offset = this.inlineDirection < 0 ? this.items[slot.index].w - this.width : 0;
            slot.content.style.transform = `translate3d(${pan.x + offset}px,${pan.y}px,0) scale(${zoom})`;
        }

        bounds(zoom) {
            const item = this.items[this.index];
            const width = (item.w || 0) * zoom;
            const height = (item.h || 0) * zoom;
            const areaHeight = this.current.areaHeight || this.height;
            return {
                minX: Math.min(0, (this.width - width) / 2, this.width - width),
                maxX: Math.max(0, (this.width - width) / 2),
                minY: Math.min(0, (areaHeight - height) / 2, areaHeight - height),
                maxY: Math.max(0, (areaHeight - height) / 2),
                centerX: (this.width - width) / 2,
                centerY: (areaHeight - height) / 2,
                width, height, areaHeight
            };
        }

        boundedPan(pan, zoom, friction = 0) {
            const bounds = this.bounds(zoom);
            const axis = (value, min, max, center, size, area) => {
                if (size <= area) return center;
                const bounded = clamp(value, min, max);
                return bounded + (value - bounded) * friction;
            };
            return {
                x: axis(pan.x, bounds.minX, bounds.maxX, bounds.centerX, bounds.width, this.width),
                y: axis(pan.y, bounds.minY, bounds.maxY, bounds.centerY, bounds.height, bounds.areaHeight)
            };
        }

        paint() {
            if (!this.current) return;
            const zoomable = this.items[this.index].type !== 'video' && !this.current.error;
            if (zoomable) this.transformImage(this.current, this.pan, this.zoom);
            const zoomButton = this.element.querySelector('[data-lightbox-action="zoom"]');
            zoomButton.disabled = !zoomable || (this.current.fit >= 1 && this.zoom <= this.current.fit + 0.01);
            if (zoomButton.disabled && document.activeElement === zoomButton) {
                this.element.querySelector('[data-lightbox-action="close"]').focus({ preventScroll: true });
            }
            this.element.classList.toggle('lightbox-zoom-allowed', zoomable && this.current.fit < 1);
            const zoomed = zoomable && this.zoom > this.current.fit + 0.01;
            this.zoomInIcon?.toggleAttribute('hidden', zoomed);
            this.zoomOutIcon?.toggleAttribute('hidden', !zoomed);
            this.element.classList.toggle('lightbox-zoomed', zoomed);
        }

        updateLoading() {
            const loader = this.loader;
            if (!loader) return;
            loader.hidden = true;
            clearTimeout(this.timers.get('loading'));
            if (this.current?.image && !this.current.loaded && this.items[this.index].type !== 'video') {
                this.later('loading', () => { loader.hidden = false; }, this.duration('loader-delay'));
            }
        }

        showControls() {
            this.synchronizeDirection();
            this.element.classList.remove('lightbox-idle', 'lightbox-controls-hidden');
            if (this.duration('idle-delay') && (!window.touchable || this.element.classList.contains('lightbox-mouse'))) {
                this.later('idle', () => this.element.classList.add('lightbox-idle'), this.duration('idle-delay'));
            }
        }

        notifyChange() {
            this.options.onChange?.(this.index);
            this.element.dispatchEvent(new CustomEvent('change.sm.lightbox', { detail: { index: this.index } }));
        }

        getCurrentIndex() { return this.legacy?.viewer.getCurrentIndex() ?? this.index; }
        next() { this.finishAnimation(); this.goTo(this.index + 1); }
        prev() { this.finishAnimation(); this.goTo(this.index - 1); }

        goTo(index, gesture = false) {
            if (!this.isOpen || this.closing) return;
            if (this.legacy) { this.legacy.viewer.goTo(index); return; }
            this.synchronizeDirection();
            this.finishAnimation();
            const next = this.normalize(index, gesture);
            if (next === this.index) { this.settle(); return; }
            this.pauseVideos();
            const previous = this.index;
            const direction = index > previous ? 1 : -1;
            const adjacent = Math.abs(next - previous) === 1 || (this.items.length > 2 && this.options.loop);
            const start = this.trackX || 0;
            // Keep the old immediate first/last keyboard wrap. Swipes use recycling holders.
            const wrap = Math.abs(next - previous) > 1;
            if (adjacent && (!wrap || gesture)) {
                this.animate(this.duration(), progress => {
                    this.track.style.transform = `translate3d(${start + (-direction * this.step * this.inlineDirection - start) * progress}px,0,0)`;
                }, () => this.changeIndex(next));
            }
            else this.changeIndex(next);
        }

        changeIndex(index) {
            this.index = index;
            this.trackX = 0;
            this.resetGesture();
            this.render();
            this.writeHistory();
            this.notifyChange();
        }

        pauseVideos() {
            this.element.querySelectorAll('video').forEach(video => video.pause());
        }

        // One owned animation at a time: finish navigation before another input,
        // cancel immediately on a gesture or disposal. No untracked animation frames.
        animate(duration, update, complete = () => {}) {
            this.cancelAnimation();
            if (window.matchMedia('(prefers-reduced-motion: reduce)').matches) duration = 0;
            if (!duration) { update(1); complete(); return; }
            const started = performance.now();
            this.animationEnd = () => { update(1); complete(); };
            update(0);
            const tick = now => {
                const progress = Math.min(1, (now - started) / duration);
                update(1 - Math.pow(1 - progress, 3));
                if (progress < 1) this.frame = requestAnimationFrame(tick);
                else {
                    this.frame = null;
                    this.animationEnd = null;
                    complete();
                }
            };
            this.frame = requestAnimationFrame(tick);
        }

        cancelAnimation() {
            cancelAnimationFrame(this.frame);
            this.frame = null;
            this.animationEnd = null;
        }

        finishAnimation() {
            const finish = this.animationEnd;
            this.cancelAnimation();
            finish?.();
        }

        animateOpening() {
            const thumb = this.options.getThumbnailRect?.(this.index);
            const item = this.items[this.index];
            const target = { ...this.pan };
            const fit = this.zoom;
            const canZoom = thumb?.w > 0 && item.w > 0 && item.type !== 'video' && !this.current.error;
            this.animate(this.duration(), progress => {
                this.element.style.opacity = Math.max(0.001, progress);
                if (canZoom) {
                    const initialZoom = thumb.w / item.w;
                    this.zoom = initialZoom + (fit - initialZoom) * progress;
                    this.pan = {
                        x: thumb.x + (target.x - thumb.x) * progress,
                        y: thumb.y - this.scrollY + (target.y - thumb.y + this.scrollY) * progress
                    };
                    this.paint();
                }
            });
        }

        toggleZoom(point = { x: this.width / 2, y: this.height / 2 }, mouse = true) {
            if (this.items[this.index].type === 'video' || this.current.error || this.closing) return;
            this.finishAnimation();
            const fit = this.current.fit;
            const target = this.zoom > fit + 0.01 ? fit : Math.max(fit, (mouse || fit < 0.7 ? 1 : 1.33));
            this.zoomTo(target, point);
        }

        zoomTo(zoom, point) {
            const startZoom = this.zoom;
            const start = { ...this.pan };
            const ratio = zoom / startZoom;
            const pan = this.boundedPan({ x: point.x - (point.x - start.x) * ratio, y: point.y - (point.y - start.y) * ratio }, zoom);
            this.animate(this.duration(), progress => {
                this.zoom = startZoom + (zoom - startZoom) * progress;
                this.pan = { x: start.x + (pan.x - start.x) * progress, y: start.y + (pan.y - start.y) * progress };
                this.paint();
            });
        }

        point(event) {
            const rect = this.stage.getBoundingClientRect();
            return { x: event.clientX - rect.left, y: event.clientY - rect.top };
        }

        pointerDown(event) {
            if (this.closing || event.button > 0) return;
            this.synchronizeDirection();
            if (nativeControl(event.target)) return;
            this.finishAnimation();
            clearTimeout(this.timers.get('tap'));
            const point = this.point(event);
            this.pointers.set(event.pointerId, point);
            this.stage.setPointerCapture(event.pointerId);
            if (this.pointers.size === 1) {
                this.gesture = {
                    start: point, last: point, pan: { ...this.pan }, zoom: this.zoom,
                    time: performance.now(), velocity: { x: 0, y: 0 }, moved: false, mouse: event.pointerType === 'mouse', target: event.target
                };
            }
            else if (this.pointers.size === 2 && this.items[this.index].type !== 'video' && !this.current.error) {
                const points = [...this.pointers.values()];
                this.gesture.pinch = {
                    distance: Math.max(1, distance(...points)), center: midpoint(...points), zoom: this.zoom, pan: { ...this.pan },
                    // Pinch-to-close only when the gesture started at the fitted size.
                    close: this.zoom <= this.current.fit + 0.01
                };
                this.gesture.moved = true;
            }
            this.element.classList.add('lightbox-dragging');
        }

        pointerMove(event) {
            if (!this.pointers.has(event.pointerId) || !this.gesture) return;
            const point = this.point(event);
            this.pointers.set(event.pointerId, point);
            const gesture = this.gesture;
            if (this.pointers.size >= 2 && gesture.pinch) {
                event.preventDefault();
                const points = [...this.pointers.values()].slice(0, 2);
                const center = midpoint(...points);
                const pinch = gesture.pinch;
                let zoom = pinch.zoom * distance(...points) / pinch.distance;
                const max = Math.max(this.current.fit, 1.33);
                if (zoom > max) zoom = max + (zoom - max) * 0.15;
                if (zoom < this.current.fit && !(pinch.close)) zoom = this.current.fit - (this.current.fit - zoom) * 0.15;
                this.zoom = Math.max(this.current.fit * 0.1, zoom);
                const ratio = this.zoom / pinch.zoom;
                this.pan = { x: center.x - (pinch.center.x - pinch.pan.x) * ratio, y: center.y - (pinch.center.y - pinch.pan.y) * ratio };
                this.background.style.opacity = this.cssNumber('opacity') * Math.min(1, this.zoom / this.current.fit);
                this.paint();
                return;
            }
            // Finish a pinch as one gesture; a remaining finger must not start a swipe.
            if (gesture.pinch) return;
            const dx = point.x - gesture.start.x;
            const dy = point.y - gesture.start.y;
            if (!gesture.moved && Math.hypot(dx, dy) < 10) return;
            event.preventDefault();
            gesture.moved = true;
            const now = performance.now();
            const elapsed = Math.max(1, now - gesture.time);
            gesture.velocity = { x: (point.x - gesture.last.x) / elapsed, y: (point.y - gesture.last.y) / elapsed };
            gesture.last = point;
            gesture.time = now;
            if (!gesture.mode) {
                gesture.mode = this.zoom > this.current.fit + 0.01 ? 'pan' :
                    Math.abs(dx) > Math.abs(dy) ? 'slide' : 'vertical';
            }
            if (gesture.mode === 'vertical') {
                this.track.style.transform = `translate3d(0,${dy}px,0)`;
                this.background.style.opacity = this.cssNumber('opacity') * Math.max(0, 1 - Math.abs(dy) / (this.height / 2));
            }
            else if (gesture.mode === 'slide') {
                const atEdge = this.normalize(this.index + (dx * this.inlineDirection > 0 ? -1 : 1), true) === this.index;
                this.trackX = dx * (atEdge ? 0.35 : 1);
                this.track.style.transform = `translate3d(${this.trackX}px,0,0)`;
            }
            else {
                const proposed = { x: gesture.pan.x + dx, y: gesture.pan.y + dy };
                this.pan = this.boundedPan(proposed, this.zoom, 0.35);
                // Transfer outward horizontal movement to the slide only after reaching an edge.
                const bounded = this.boundedPan(proposed, this.zoom);
                const overflow = proposed.x - bounded.x;
                if (!gesture.mouse && Math.abs(overflow) > 30 && Math.abs(dx) > Math.abs(dy) * 1.5) {
                    gesture.mode = 'slide';
                    gesture.start = { x: point.x - overflow, y: point.y };
                    this.trackX = 0;
                }
                this.paint();
            }
        }

        pointerUp(event, cancelled = false) {
            if (!this.pointers.has(event.pointerId)) return;
            this.pointers.delete(event.pointerId);
            if (this.stage.hasPointerCapture(event.pointerId)) this.stage.releasePointerCapture(event.pointerId);
            const gesture = this.gesture;
            if (!gesture) return;
            if (gesture.pinch) {
                if (this.pointers.size) return;
                if (!cancelled && gesture.pinch.close && this.zoom < this.current.fit * 0.85) this.close();
                else this.settle();
            }
            else if (this.pointers.size) return;
            else if (!gesture.moved && !cancelled) this.tap(event, gesture);
            else if (gesture.mode === 'vertical') {
                const dy = this.point(event).y - gesture.start.y;
                if (!cancelled && Math.abs(dy) / (this.height / 2) > 0.25) this.close();
                else this.settle();
            }
            else if (gesture.mode === 'slide') {
                const dx = this.trackX || 0;
                const fast = performance.now() - gesture.time < 80 && Math.abs(gesture.velocity.x) > 0.5;
                if (!cancelled && (Math.abs(dx) > this.width * 0.2 || (Math.abs(dx) > 30 && fast))) this.goTo(this.index + (dx * this.inlineDirection > 0 ? -1 : 1), true);
                else this.settle();
            }
            else this.settle(cancelled ? null : gesture.velocity);
            this.gesture = null;
            this.element.classList.remove('lightbox-dragging');
        }

        tap(event, gesture) {
            const point = this.point(event);
            if (gesture.mouse) {
                if (gesture.target.closest('.lightbox-image')) {
                    if (this.current.fit === 1 && this.zoom === 1) this.close();
                    else this.toggleZoom(point);
                }
                else this.close();
                return;
            }
            const now = performance.now();
            if (this.lastTap && now - this.lastTap.time < 300 && distance(point, this.lastTap.point) < 25) {
                this.lastTap = null;
                this.toggleZoom(point, false);
            }
            else {
                this.lastTap = { point, time: now };
                this.later('tap', () => {
                    this.element.classList.toggle('lightbox-controls-hidden');
                }, 300);
            }
        }

        settle(velocity) {
            const zoom = clamp(this.zoom, this.current.fit, Math.max(this.current.fit, 1.33));
            const startZoom = this.zoom;
            const start = { ...this.pan };
            const fresh = this.gesture && performance.now() - this.gesture.time < 80;
            const target = this.boundedPan({
                x: start.x + (fresh && velocity ? clamp(velocity.x, -3, 3) * 240 : 0),
                y: start.y + (fresh && velocity ? clamp(velocity.y, -3, 3) * 240 : 0)
            }, zoom);
            const trackX = this.trackX || 0;
            const trackY = this.gesture?.mode === 'vertical' ? this.gesture.last.y - this.gesture.start.y : 0;
            const opacity = Number(this.background.style.opacity);
            this.animate(this.duration(), progress => {
                this.zoom = startZoom + (zoom - startZoom) * progress;
                this.pan = { x: start.x + (target.x - start.x) * progress, y: start.y + (target.y - start.y) * progress };
                this.track.style.transform = `translate3d(${trackX * (1 - progress)}px,${trackY * (1 - progress)}px,0)`;
                this.background.style.opacity = opacity + (this.cssNumber('opacity') - opacity) * progress;
                this.paint();
            }, () => { this.trackX = 0; });
        }

        resetGesture() {
            this.releasePointers();
            this.gesture = null;
            this.lastTap = null;
            this.trackX = 0;
            this.element.classList.remove('lightbox-dragging');
            this.background.style.opacity = this.cssNumber('opacity');
        }

        releasePointers() {
            const ids = [...this.pointers.keys()];
            this.pointers.clear();
            for (const id of ids) {
                if (this.stage?.hasPointerCapture(id)) this.stage.releasePointerCapture(id);
            }
        }

        wheel(event) {
            if (event.target.closest('video') || this.closing) return;
            // Preserve desktop wheel panning/closing rather than changing it to zoom.
            if (this.zoom > this.current.fit + 0.01) {
                event.preventDefault();
                this.finishAnimation();
                const unit = event.deltaMode === 1 ? 18 : event.deltaMode === 2 ? this.height : 1;
                this.pan = this.boundedPan({ x: this.pan.x - event.deltaX * unit, y: this.pan.y - event.deltaY * unit }, this.zoom);
                this.paint();
            }
            else if (this.options.closeOnScroll && !document.fullscreenElement && Math.abs(event.deltaY) > 2) this.close();
            else event.preventDefault();
        }

        updateFullscreen() {
            const fullscreen = document.fullscreenElement === this.element;
            const button = this.element.querySelector('[data-lightbox-action="fullscreen"]');
            if (button) button.hidden = !document.fullscreenEnabled;
            this.fullscreenEnterIcon?.toggleAttribute('hidden', fullscreen);
            this.fullscreenExitIcon?.toggleAttribute('hidden', !fullscreen);
        }

        async toggleFullscreen() {
            try {
                if (document.fullscreenElement === this.element) await document.exitFullscreen();
                else if (!document.fullscreenElement) await this.element.requestFullscreen();
            }
            catch { /* Fullscreen can be denied by the embedding page or browser. */ }
        }

        openHistory() {
            if (!this.options.history) return;
            this.originalURL = location.href;
            this.historyID = String(this.options.history === true ? 1 : this.options.history);
            const hash = new URLSearchParams(location.hash.slice(1));
            const initialIndex = Number(hash.get('pid')) - 1;
            this.historyPushed = hash.get('gid') !== this.historyID;
            if (!this.historyPushed && Number.isInteger(initialIndex) && initialIndex >= 0 && initialIndex < this.items.length) {
                this.index = initialIndex;
                this.render();
            }
            this.originalHash = new URLSearchParams(location.hash.slice(1));
            this.originalHash.delete('gid');
            this.originalHash.delete('pid');
            this.writeHistory(this.historyPushed);
            const changed = () => {
                const hash = new URLSearchParams(location.hash.slice(1));
                if (hash.get('gid') !== this.historyID) this.close(true);
                else {
                    const index = Number(hash.get('pid')) - 1;
                    if (Number.isInteger(index) && index >= 0 && index < this.items.length && index !== this.index) this.goTo(index);
                }
            };
            this.on(window, 'popstate', changed);
            this.on(window, 'hashchange', changed);
        }

        writeHistory(push = false) {
            if (!this.historyID || this.closing) return;
            const hash = new URLSearchParams(this.originalHash);
            hash.set('gid', this.historyID);
            hash.set('pid', this.index + 1);
            const url = new URL(location.href);
            url.hash = hash.toString();
            history[push ? 'pushState' : 'replaceState'](history.state, '', url);
        }

        restoreHistory(fromHistory, immediate) {
            if (!this.historyID || fromHistory) return;
            if (this.historyPushed && !immediate) history.back();
            else {
                const url = new URL(this.originalURL);
                url.hash = this.originalHash.toString();
                history.replaceState(history.state, '', url);
            }
            this.historyID = null;
        }

        close(fromHistory = false, immediate = false) {
            if (!this.isOpen || this.closing) return;
            if (this.legacy) { this.legacy.close(immediate); return; }
            this.closing = true;
            this.finishAnimation();
            this.events.abort();
            this.pauseVideos();
            this.restoreHistory(fromHistory, immediate);
            if (document.fullscreenElement === this.element) document.exitFullscreen().catch(() => {});
            const opacity = Number(this.element.style.opacity);
            const startZoom = this.zoom;
            const start = { ...this.pan };
            const item = this.items[this.index];
            const thumb = this.options.getThumbnailRect?.(this.index);
            this.animate(immediate ? 0 : this.duration(), progress => {
                this.element.style.opacity = opacity * (1 - progress);
                if (thumb?.w && item.w && item.type !== 'video' && !this.current.error) {
                    this.zoom = startZoom + (thumb.w / item.w - startZoom) * progress;
                    this.pan = { x: start.x + (thumb.x - start.x) * progress, y: start.y + (thumb.y - window.scrollY - start.y) * progress };
                    this.paint();
                }
            }, () => this.dispose());
        }

        dispose() {
            this.cancelAnimation();
            this.timers.forEach(clearTimeout);
            this.timers.clear();
            this.events?.abort();
            this.slots?.forEach(slot => this.clearSlot(slot));
            if (this.loader) {
                this.loader.hidden = true;
                this.loader.replaceChildren();
                this.loader = null;
            }
            this.releasePointers();
            this.element.hidden = true;
            this.element.setAttribute('aria-hidden', 'true');
            this.isOpen = false;
            this.closing = false;
            this.legacy = null;
            if (active === this) {
                window.AccessKitFocusTrap?.deactivate();
                active = null;
            }
            instances.delete(this.element);
            this.options.onClose?.();
            this.element.dispatchEvent(new CustomEvent('close.sm.lightbox'));
        }

        destroy() {
            if (this.closing) { this.finishAnimation(); return; }
            this.close(false, true);
        }
    }

    // Kept here so Gallery only supplies media and synchronizes its selected index.
    // This adapter can be removed after the parallel PhotoSwipe test phase.
    class LegacyLightbox {
        constructor(owner) { this.owner = owner; }

        open() {
            const owner = this.owner;
            const root = document.getElementById('pswp');
            const container = root.querySelector('.pswp__container');
            this.events = new AbortController();
            const on = (type, callback) => root.addEventListener(type, callback, { signal: this.events.signal });
            const pause = () => root.querySelectorAll('video').forEach(video => video.pause());
            const transition = direction => {
                const index = this.viewer.getCurrentIndex();
                container.classList.toggle('sliding', !((direction < 0 && index === 0) || (direction > 0 && index === owner.items.length - 1)));
            };
            on('keydown', event => {
                if (event.key === 'ArrowLeft' || event.key === 'ArrowRight') transition(event.key === 'ArrowLeft' ? -1 : 1);
            });
            on('mousedown', event => {
                const arrow = event.target.closest('.pswp__arrow');
                if (arrow) {
                    event.stopPropagation();
                    transition(arrow.classList.contains('pswp__button--arrow--left') ? -1 : 1);
                }
                else container.classList.remove('sliding');
            });
            on('dblclick', event => {
                if (event.target.closest('.pswp__arrow')) { event.preventDefault(); event.stopPropagation(); }
            });
            container.addEventListener('transitionend', () => container.classList.remove('sliding'), { signal: this.events.signal });
            const items = owner.items.map(item => {
                if (item.type !== 'video') return item;
                const content = document.createElement('div');
                content.className = 'video-container hstack justify-content-center';
                const video = document.createElement('video');
                video.className = 'video-item';
                video.src = item.src;
                video.controls = true;
                video.preload = 'metadata';
                content.append(video);
                return { html: content.outerHTML };
            });
            this.viewer = new PhotoSwipe(root, PhotoSwipeUI_Default, items, {
                index: owner.index,
                loop: owner.options.loop,
                history: !!owner.options.history,
                galleryUID: owner.options.history === true ? 1 : owner.options.history,
                closeOnScroll: owner.options.closeOnScroll,
                showHideOpacity: true,
                shareEl: false,
                barsSize: { top: 0, bottom: 'auto' },
                getThumbBoundsFn: owner.options.getThumbnailRect,
                showAnimationDuration: owner.duration(),
                hideAnimationDuration: owner.duration()
            });
            this.viewer.listen('beforeChange', pause);
            this.viewer.listen('afterChange', () => {
                owner.index = this.viewer.getCurrentIndex();
                for (const slide of container.children) {
                    const current = slide.contains(this.viewer.currItem.container);
                    slide.setAttribute('aria-hidden', String(!current));
                    slide.inert = !current;
                }
                owner.notifyChange();
            });
            this.viewer.listen('destroy', () => {
                pause();
                this.events.abort();
                container.classList.remove('sliding');
                owner.dispose();
            });
            this.viewer.init();
            window.AccessKitFocusTrap?.activate(root, { exclude: '.pswp__item[aria-hidden="true"] *' });
        }

        close(immediate) {
            if (immediate) this.viewer.options.hideAnimationDuration = 0;
            this.viewer.close();
        }
    }

    namespace.Lightbox = Lightbox;
})();
