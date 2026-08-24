---
name: Titanium POS
colors:
  surface: '#131313'
  surface-dim: '#131313'
  surface-bright: '#3a3939'
  surface-container-lowest: '#0e0e0e'
  surface-container-low: '#1c1b1b'
  surface-container: '#201f1f'
  surface-container-high: '#2a2a2a'
  surface-container-highest: '#353534'
  on-surface: '#e5e2e1'
  on-surface-variant: '#e7bcba'
  inverse-surface: '#e5e2e1'
  inverse-on-surface: '#313030'
  outline: '#ae8885'
  outline-variant: '#5d3f3d'
  surface-tint: '#ffb3af'
  primary: '#ffb3af'
  on-primary: '#68000e'
  primary-container: '#d90429'
  on-primary-container: '#ffeae8'
  inverse-primary: '#bf0022'
  secondary: '#c6c6c7'
  on-secondary: '#2f3131'
  secondary-container: '#454747'
  on-secondary-container: '#b4b5b5'
  tertiary: '#4ae176'
  on-tertiary: '#003915'
  tertiary-container: '#007d36'
  on-tertiary-container: '#b9ffbf'
  error: '#ffb4ab'
  on-error: '#690005'
  error-container: '#93000a'
  on-error-container: '#ffdad6'
  primary-fixed: '#ffdad7'
  primary-fixed-dim: '#ffb3af'
  on-primary-fixed: '#410005'
  on-primary-fixed-variant: '#930018'
  secondary-fixed: '#e2e2e2'
  secondary-fixed-dim: '#c6c6c7'
  on-secondary-fixed: '#1a1c1c'
  on-secondary-fixed-variant: '#454747'
  tertiary-fixed: '#6bff8f'
  tertiary-fixed-dim: '#4ae176'
  on-tertiary-fixed: '#002109'
  on-tertiary-fixed-variant: '#005321'
  background: '#131313'
  on-background: '#e5e2e1'
  surface-variant: '#353534'
typography:
  display-lg:
    fontFamily: Hanken Grotesk
    fontSize: 48px
    fontWeight: '700'
    lineHeight: 56px
    letterSpacing: -0.02em
  headline-lg:
    fontFamily: Hanken Grotesk
    fontSize: 32px
    fontWeight: '600'
    lineHeight: 40px
    letterSpacing: -0.01em
  headline-md:
    fontFamily: Hanken Grotesk
    fontSize: 24px
    fontWeight: '600'
    lineHeight: 32px
  title-md:
    fontFamily: Hanken Grotesk
    fontSize: 18px
    fontWeight: '600'
    lineHeight: 24px
  body-lg:
    fontFamily: Hanken Grotesk
    fontSize: 16px
    fontWeight: '400'
    lineHeight: 24px
  body-md:
    fontFamily: Hanken Grotesk
    fontSize: 14px
    fontWeight: '400'
    lineHeight: 20px
  label-md:
    fontFamily: JetBrains Mono
    fontSize: 12px
    fontWeight: '500'
    lineHeight: 16px
    letterSpacing: 0.02em
  headline-lg-mobile:
    fontFamily: Hanken Grotesk
    fontSize: 24px
    fontWeight: '600'
    lineHeight: 32px
rounded:
  sm: 0.25rem
  DEFAULT: 0.5rem
  md: 0.75rem
  lg: 1rem
  xl: 1.5rem
  full: 9999px
spacing:
  base: 4px
  xs: 4px
  sm: 8px
  md: 16px
  lg: 24px
  xl: 48px
  gutter: 16px
  margin: 24px
---

## Brand & Style

This design system is engineered for high-performance retail and enterprise environments. It prioritizes speed, clarity, and a "mission-control" aesthetic. The brand personality is authoritative yet sophisticated, utilizing a dark-mode-first approach to reduce eye strain for operators during long shifts.

The design style is **Modern Corporate with Glassmorphic accents**. It borrows the precision of a developer tool (JetBrains) and the aesthetic polish of high-end fintech (Stripe). Key characteristics include:
- **High-Density Utility**: Efficient use of screen real estate for complex data management.
- **Subtle Depth**: Utilizing translucent layers and background blurs (12px–20px) to maintain context without visual clutter.
- **Precision Accents**: A vibrant primary red is used sparingly to draw attention to critical actions and active states against a monochromatic backdrop.

## Colors

The palette is anchored by a deep obsidian background to establish a premium feel. 

- **Primary Red (#D90429)**: Reserved exclusively for "Action" states, such as completing a sale, primary buttons, and active navigation indicators.
- **Surface Hierarchy**: Use `#1A1A1A` for the main content area and `#222222` for elevated elements like modals or popovers.
- **Borders**: All structural separation is handled by `#2C2C2C`. Avoid using solid black for borders; they must remain visible against the `#111111` background.
- **Translucency**: When applying glassmorphism, use the surface colors with an 80% opacity and a `backdrop-filter: blur(12px)`.

## Typography

This system uses **Hanken Grotesk** for its modern, sharp geometric profile that mirrors the technical nature of an ERP. **JetBrains Mono** is introduced for tabular data, SKU numbers, and status labels to provide a functional, "pro-tool" feel.

- **Contrast**: Use `text-primary` for all headings and `text-secondary` for descriptions and metadata.
- **Numeric Data**: Always use tabular figures (monospaced) for prices and stock counts to ensure alignment in lists.
- **Scaling**: Headlines should shift to their `-mobile` variants below 768px.

## Layout & Spacing

The system follows a **12-column fluid grid** for the main dashboard content, while the POS terminal interface uses a **fixed-component layout** to ensure hit targets remain consistent for touch interfaces.

- **Sidebar**: Fixed at 240px when expanded, 64px when collapsed.
- **Safe Areas**: Use a 24px margin on all screen edges for desktop, reducing to 16px on mobile.
- **Data Density**: Elements in the DataGrid should use `sm` (8px) vertical padding to maximize information density, while Cards should use `lg` (24px) padding to feel premium and spacious.

## Elevation & Depth

Visual hierarchy is established through a combination of **Tonal Layering** and **Subtle Shadows**.

1.  **Level 0 (Background)**: `#111111` – The base canvas.
2.  **Level 1 (Cards/Sidebar)**: `#1A1A1A` – For primary containers. Use a 1px solid border of `#2C2C2C`.
3.  **Level 2 (Modals/Hover States)**: `#222222` – For elements appearing over Level 1.
4.  **Shadows**: Use a single, deep ambient shadow for elevated surfaces: `0 20px 40px rgba(0,0,0,0.4)`. 
5.  **Glass Effect**: Apply to top navigation bars and filter panels. Use `rgba(26, 26, 26, 0.8)` with a `12px` blur and a `top-border` highlight of `white` at `0.05` opacity to simulate a light-catching edge.

## Shapes

The system uses a variable rounding scale to balance "professional" and "modern."

- **Large Containers (Cards, Modals)**: Use `rounded-xl` (1.5rem / 24px) or `rounded-lg` (1rem / 16px) to soften the large dark surfaces.
- **Interactive Elements (Buttons, Inputs, Chips)**: Use `rounded-md` (0.5rem / 8px) to maintain a precise, tool-like appearance.
- **Status Indicators**: Use pill-shapes (full rounding) for status chips to distinguish them from clickable buttons.

## Components

### Buttons
- **Primary**: Solid `#D90429` with white text. On hover, use `#EF233C`.
- **Secondary**: Outlined with `#2C2C2C`, transparent background. On hover, background becomes `#222222`.
- **Ghost**: No border or background. Red text for destructive actions, White for neutral.

### DataGrid
- **Header**: Sticky, `#1A1A1A` background with a bottom border of `#2C2C2C`.
- **Rows**: `#111111` background. On hover, transition to `#1A1A1A`. 
- **Cells**: Use `label-md` (Mono font) for numeric columns.

### Sidebar
- **Active State**: Background is transparent, but features a 3px vertical "pill" of Primary Red on the left edge. Icon and text color change to White.
- **Icons**: 20px stroke-based icons.

### Inputs & Search
- **Default**: Background `#1A1A1A`, border 1px solid `#2C2C2C`.
- **Focus**: Border color transitions to Primary Red with a subtle red outer glow (`box-shadow: 0 0 0 2px rgba(217, 4, 41, 0.2)`).

### Cards (KPIs)
- Features a subtle 1px border highlight.
- Trend indicators (up/down) use `Success` or `Danger` colors.
- Large numerical values should use `headline-lg`.

### Status Chips
- Small, uppercase labels using `label-md`. 
- Background is a 10% opacity version of the semantic color (e.g., Success), with a solid 1px border of the same color at 30% opacity.