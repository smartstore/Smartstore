### Bootstrap Icons 1.10.0

To keep file size small, the Smartstore backend uses only a subset of [Bootstrap Icons 1.11.0](https://icons.getbootstrap.com/#icons):

- Nearly all `*-fill` variants (except such trivial icons like `square-fill` etc.) have been removed.

- All variants that can be easily created with flip/rotate - e.g. `arrow-left`, which is simply a flipped variant of `arrow-right` - have been removed. Mostly we removed the `left`- and -`bottom` variants.

  

The following CSS classes are available for icon transformation:

- `.rotate-90`
- `.rotate-180`
- `.rotate-270`
- `.flip-h`
- `.flip-v`
- `.flip-hv`



To animate icons:

- `.animate-fade`
- `.animate-spin`
- `.animate-spin-reverse`
- `.animate-spin-pulse`
- `.animate-spin-pulse-reverse`
- `.animate-beat`
- `.animate-throb`
- `.animate-cylon`
- `.animate-cylon-vertical`



Other helper classes:

- `.bi-fw` (makes icon fixed width)
- `.bi-ul` (icons in a list)



#### Tag Helper

```html
<bootstrap-icon
    <!-- Name of icon (required) -->
    name="trash"
    <!-- Extra CSS classes for the root <svg> element -->
    class="bi-fw"
    <!-- Extra CSS styles for the root <svg> element -->
    style="color: #000"
	<!-- Animation type: Fade | Spin | SpinReverse | SpinPulse | SpinPulseReverse | Beat | Throb | Cylon | CylonVertical -->
    animation="CssAnimation.Spin"
    fill="currentColor (default)"
    font-scale="[float]"
    scale="[float]"
    flip-h="[bool]"
    flip-v="[bool]"
    shift-x="[float]"
    shift-y="[float]"
    rotate="[degrees]"/>
```

```html
<bootstrap-iconstack
    <!-- Extra CSS classes for the root <svg> element -->
    class="bi-fw"
    <!-- Extra CSS styles for the root <svg> element -->
    style="color: #000"
	<!-- Animation type: Fade | Spin | SpinReverse | SpinPulse | SpinPulseReverse | Beat | Throb | Cylon | CylonVertical -->
    animation="CssAnimation.Spin"
    fill="currentColor (default)"
    font-scale="[float]"
    scale="[float]"
    flip-h="[bool]"
    flip-v="[bool]"
    shift-x="[float]"
    shift-y="[float]"
    rotate="[degrees]">
    <bootstrap-icon ... />
    <bootstrap-icon ... />
    ...
</bootstrap-iconstack>
```

