# Platform Style Guide

## Automated Market Intelligence Tool

**Version:** 1.0.0
**Last Updated:** January 2026

---

## Table of Contents

1. [Design Philosophy](#design-philosophy)
2. [Color System](#color-system)
3. [Typography](#typography)
4. [Spacing System](#spacing-system)
5. [Layout & Grid](#layout--grid)
6. [Border Radius](#border-radius)
7. [Shadows & Elevation](#shadows--elevation)
8. [Components](#components)
9. [Iconography](#iconography)
10. [Accessibility](#accessibility)
11. [Design Tokens](#design-tokens)
12. [CSS Variables Reference](#css-variables-reference)

---

## Design Philosophy

The platform design philosophy centers on **trust**, **transparency**, and **ease of use**. Every design decision should:

- **Build Trust**: Use the signature blue color palette to convey reliability and professionalism
- **Ensure Clarity**: Present information in a clear, organized hierarchy
- **Enable Action**: Make CTAs prominent and accessible
- **Maintain Consistency**: Use established patterns across all touchpoints
- **Prioritize Accessibility**: Meet or exceed WCAG 2.1 AA standards

---

## Color System

### Primary Brand Colors

| Color Name | Hex | RGB | CMYK | Usage |
|------------|-----|-----|------|-------|
| **Primary Blue** | `#00529F` | rgb(0, 82, 159) | 100, 48, 0, 38 | Primary brand color, headers, primary buttons, links |
| **Accent Yellow** | `#FFD520` | rgb(255, 213, 32) | 0, 16, 87, 0 | Accents, highlights, promotional elements, CTAs |
| **White** | `#FFFFFF` | rgb(255, 255, 255) | 0, 0, 0, 0 | Backgrounds, text on dark surfaces |

### Extended Blue Palette

| Token Name | Hex | Usage |
|------------|-----|-------|
| `--color-blue-900` | `#001F3F` | Darkest blue, footer backgrounds |
| `--color-blue-800` | `#003366` | Dark blue, hover states |
| `--color-blue-700` | `#00529F` | **Primary brand blue** |
| `--color-blue-600` | `#0066CC` | Interactive elements |
| `--color-blue-500` | `#1976D2` | Links, secondary actions |
| `--color-blue-400` | `#42A5F5` | Light accents |
| `--color-blue-300` | `#90CAF9` | Backgrounds, disabled states |
| `--color-blue-200` | `#BBDEFB` | Light backgrounds |
| `--color-blue-100` | `#E3F2FD` | Subtle backgrounds, hover states |
| `--color-blue-50` | `#F5F9FF` | Lightest blue tint |

### Extended Yellow Palette

| Token Name | Hex | Usage |
|------------|-----|-------|
| `--color-yellow-900` | `#B8860B` | Dark gold, warning states |
| `--color-yellow-800` | `#DAA520` | Dark yellow accents |
| `--color-yellow-700` | `#FFD520` | **Primary brand yellow** |
| `--color-yellow-600` | `#FFDD4B` | Standard yellow |
| `--color-yellow-500` | `#FFE566` | Light yellow |
| `--color-yellow-400` | `#FFEB99` | Pale yellow highlights |
| `--color-yellow-300` | `#FFF2CC` | Very light backgrounds |
| `--color-yellow-200` | `#FFF8E1` | Subtle yellow tint |
| `--color-yellow-100` | `#FFFDE7` | Lightest yellow |

### Neutral/Gray Palette

| Token Name | Hex | RGB | Usage |
|------------|-----|-----|-------|
| `--color-gray-900` | `#1A1A1A` | rgb(26, 26, 26) | Primary text, headings |
| `--color-gray-800` | `#333333` | rgb(51, 51, 51) | Body text |
| `--color-gray-700` | `#4D4D4D` | rgb(77, 77, 77) | Secondary text |
| `--color-gray-600` | `#666666` | rgb(102, 102, 102) | Placeholder text |
| `--color-gray-500` | `#808080` | rgb(128, 128, 128) | Disabled text |
| `--color-gray-400` | `#999999` | rgb(153, 153, 153) | Icons, borders |
| `--color-gray-300` | `#CCCCCC` | rgb(204, 204, 204) | Dividers, borders |
| `--color-gray-200` | `#E5E5E5` | rgb(229, 229, 229) | Light borders |
| `--color-gray-100` | `#F2F2F2` | rgb(242, 242, 242) | Background fills |
| `--color-gray-50` | `#FAFAFA` | rgb(250, 250, 250) | Subtle backgrounds |

### Semantic Colors

| Token Name | Hex | Usage |
|------------|-----|-------|
| `--color-success` | `#2E7D32` | Success states, confirmations |
| `--color-success-light` | `#E8F5E9` | Success backgrounds |
| `--color-warning` | `#ED6C02` | Warning states, caution |
| `--color-warning-light` | `#FFF3E0` | Warning backgrounds |
| `--color-error` | `#D32F2F` | Error states, validation |
| `--color-error-light` | `#FFEBEE` | Error backgrounds |
| `--color-info` | `#0288D1` | Informational states |
| `--color-info-light` | `#E1F5FE` | Info backgrounds |

### Interactive State Colors

| State | Primary Button | Secondary Button | Link |
|-------|---------------|------------------|------|
| **Default** | `#00529F` | `transparent` | `#00529F` |
| **Hover** | `#003366` | `#E3F2FD` | `#003366` |
| **Active** | `#001F3F` | `#BBDEFB` | `#001F3F` |
| **Focus** | `#00529F` + focus ring | `transparent` + focus ring | `#00529F` |
| **Disabled** | `#CCCCCC` | `#F2F2F2` | `#999999` |

---

## Typography

### Font Families

```css
/* Primary Brand Font - Headlines & Display */
--font-family-primary: 'Sharp Sans', 'Montserrat', -apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif;

/* Secondary Font - Body & UI */
--font-family-secondary: 'Lato', 'Roboto', -apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif;

/* Monospace - Code & Technical */
--font-family-mono: 'SF Mono', 'Monaco', 'Inconsolata', 'Fira Mono', monospace;
```

### Font Weights

| Token | Value | Usage |
|-------|-------|-------|
| `--font-weight-light` | 300 | Large display text |
| `--font-weight-regular` | 400 | Body text, paragraphs |
| `--font-weight-medium` | 500 | Subheadings, emphasis |
| `--font-weight-semibold` | 600 | Buttons, labels |
| `--font-weight-bold` | 700 | Headlines, strong emphasis |
| `--font-weight-extrabold` | 800 | Display headlines |

### Type Scale

| Token | Size (px) | Size (rem) | Line Height | Letter Spacing | Usage |
|-------|-----------|------------|-------------|----------------|-------|
| `--text-display-xl` | 72px | 4.5rem | 1.1 | -0.02em | Hero headlines |
| `--text-display-lg` | 60px | 3.75rem | 1.1 | -0.02em | Section heroes |
| `--text-display-md` | 48px | 3rem | 1.2 | -0.01em | Page titles |
| `--text-display-sm` | 36px | 2.25rem | 1.2 | -0.01em | Large headings |
| `--text-h1` | 32px | 2rem | 1.25 | -0.01em | H1 headings |
| `--text-h2` | 28px | 1.75rem | 1.3 | -0.005em | H2 headings |
| `--text-h3` | 24px | 1.5rem | 1.3 | 0 | H3 headings |
| `--text-h4` | 20px | 1.25rem | 1.4 | 0 | H4 headings |
| `--text-h5` | 18px | 1.125rem | 1.4 | 0 | H5 headings |
| `--text-h6` | 16px | 1rem | 1.4 | 0.01em | H6 headings |
| `--text-body-lg` | 18px | 1.125rem | 1.6 | 0 | Large body text |
| `--text-body-md` | 16px | 1rem | 1.6 | 0 | Default body text |
| `--text-body-sm` | 14px | 0.875rem | 1.5 | 0.01em | Small body text |
| `--text-caption` | 12px | 0.75rem | 1.4 | 0.02em | Captions, labels |
| `--text-overline` | 12px | 0.75rem | 1.4 | 0.1em | Overlines (uppercase) |
| `--text-micro` | 10px | 0.625rem | 1.4 | 0.03em | Fine print |

### Typography Styles

```css
/* Display Styles */
.text-display-xl {
  font-family: var(--font-family-primary);
  font-size: var(--text-display-xl);
  font-weight: var(--font-weight-bold);
  line-height: 1.1;
  letter-spacing: -0.02em;
}

/* Heading Styles */
.text-h1 {
  font-family: var(--font-family-primary);
  font-size: var(--text-h1);
  font-weight: var(--font-weight-bold);
  line-height: 1.25;
  letter-spacing: -0.01em;
  color: var(--color-gray-900);
}

/* Body Styles */
.text-body {
  font-family: var(--font-family-secondary);
  font-size: var(--text-body-md);
  font-weight: var(--font-weight-regular);
  line-height: 1.6;
  color: var(--color-gray-800);
}
```

---

## Spacing System

The spacing system uses an 8px base unit with a modular scale for consistent rhythm.

### Spacing Scale

| Token | Value | Usage |
|-------|-------|-------|
| `--spacing-0` | 0 | No spacing |
| `--spacing-1` | 4px | Tight spacing, icons |
| `--spacing-2` | 8px | **Base unit**, compact elements |
| `--spacing-3` | 12px | Small gaps |
| `--spacing-4` | 16px | Standard element spacing |
| `--spacing-5` | 20px | Medium gaps |
| `--spacing-6` | 24px | Section padding |
| `--spacing-8` | 32px | Large gaps |
| `--spacing-10` | 40px | Component separation |
| `--spacing-12` | 48px | Section margins |
| `--spacing-16` | 64px | Large section spacing |
| `--spacing-20` | 80px | Hero/section padding |
| `--spacing-24` | 96px | Page-level spacing |
| `--spacing-32` | 128px | Major section breaks |

### Component Spacing

| Component | Padding | Margin | Gap |
|-----------|---------|--------|-----|
| **Button (sm)** | 8px 16px | - | 8px |
| **Button (md)** | 12px 24px | - | 8px |
| **Button (lg)** | 16px 32px | - | 12px |
| **Card** | 24px | 16px | 16px |
| **Form Field** | 12px 16px | 0 0 24px | - |
| **Modal** | 24px | - | 16px |
| **Section** | 64px 0 | - | 32px |
| **Container** | 0 24px | 0 auto | - |

### Spacing Usage Guidelines

```css
/* Component internal padding */
.card {
  padding: var(--spacing-6); /* 24px */
}

/* Spacing between related elements */
.form-group {
  margin-bottom: var(--spacing-6); /* 24px */
}

/* Spacing between sections */
.section {
  padding: var(--spacing-16) 0; /* 64px */
}

/* Gap in flex/grid layouts */
.button-group {
  gap: var(--spacing-3); /* 12px */
}
```

---

## Layout & Grid

### Breakpoints

| Token | Value | Description |
|-------|-------|-------------|
| `--breakpoint-xs` | 0 | Extra small devices (phones) |
| `--breakpoint-sm` | 576px | Small devices (landscape phones) |
| `--breakpoint-md` | 768px | Medium devices (tablets) |
| `--breakpoint-lg` | 992px | Large devices (desktops) |
| `--breakpoint-xl` | 1200px | Extra large devices (large desktops) |
| `--breakpoint-xxl` | 1400px | Extra extra large devices |

### Container Widths

| Breakpoint | Max Width | Padding |
|------------|-----------|---------|
| xs (< 576px) | 100% | 16px |
| sm (≥ 576px) | 540px | 24px |
| md (≥ 768px) | 720px | 24px |
| lg (≥ 992px) | 960px | 24px |
| xl (≥ 1200px) | **1200px** | 24px |
| xxl (≥ 1400px) | 1200px | 24px |

> **Note:** Maximum content width is capped at 1200px (large breakpoint) to maintain readability and digestible content views.

### Grid System

```css
/* 12-column grid */
.grid {
  display: grid;
  grid-template-columns: repeat(12, 1fr);
  gap: var(--spacing-6); /* 24px gutter */
}

/* Responsive columns */
@media (max-width: 767px) {
  .grid {
    grid-template-columns: repeat(4, 1fr);
    gap: var(--spacing-4); /* 16px gutter */
  }
}

@media (min-width: 768px) and (max-width: 991px) {
  .grid {
    grid-template-columns: repeat(8, 1fr);
  }
}
```

### Layout Patterns

| Pattern | Columns (Desktop) | Columns (Tablet) | Columns (Mobile) |
|---------|-------------------|------------------|------------------|
| Full Width | 12 | 8 | 4 |
| Two Column | 6 + 6 | 4 + 4 | 4 (stacked) |
| Three Column | 4 + 4 + 4 | 4 + 4 (+ 8) | 4 (stacked) |
| Sidebar | 3 + 9 | 3 + 5 | 4 (stacked) |
| Card Grid | 4 + 4 + 4 | 4 + 4 | 4 (stacked) |

---

## Border Radius

### Radius Scale

| Token | Value | Usage |
|-------|-------|-------|
| `--radius-none` | 0 | Sharp corners |
| `--radius-sm` | 4px | Small elements, chips |
| `--radius-md` | 8px | **Default**, buttons, inputs, cards |
| `--radius-lg` | 12px | Larger cards, modals |
| `--radius-xl` | 16px | Featured elements |
| `--radius-2xl` | 24px | Large promotional cards |
| `--radius-full` | 9999px | Pills, circular avatars |

### Component Radius

| Component | Border Radius |
|-----------|---------------|
| Button | `--radius-md` (8px) |
| Input/Text Field | `--radius-md` (8px) |
| Card | `--radius-lg` (12px) |
| Modal | `--radius-lg` (12px) |
| Dropdown | `--radius-md` (8px) |
| Chip/Tag | `--radius-full` (pill) |
| Avatar | `--radius-full` (circle) |
| Tooltip | `--radius-sm` (4px) |
| Alert/Banner | `--radius-md` (8px) |

---

## Shadows & Elevation

### Shadow Scale

| Token | Value | Elevation Level | Usage |
|-------|-------|-----------------|-------|
| `--shadow-none` | none | 0 | Flat elements |
| `--shadow-xs` | `0 1px 2px rgba(0, 0, 0, 0.05)` | 1 | Subtle lift |
| `--shadow-sm` | `0 1px 3px rgba(0, 0, 0, 0.1), 0 1px 2px rgba(0, 0, 0, 0.06)` | 2 | Cards, buttons |
| `--shadow-md` | `0 4px 6px rgba(0, 0, 0, 0.1), 0 2px 4px rgba(0, 0, 0, 0.06)` | 3 | Dropdowns, hover cards |
| `--shadow-lg` | `0 10px 15px rgba(0, 0, 0, 0.1), 0 4px 6px rgba(0, 0, 0, 0.05)` | 4 | Modals, popovers |
| `--shadow-xl` | `0 20px 25px rgba(0, 0, 0, 0.1), 0 10px 10px rgba(0, 0, 0, 0.04)` | 5 | Dialogs |
| `--shadow-2xl` | `0 25px 50px rgba(0, 0, 0, 0.25)` | 6 | Overlays |

### Focus Ring

```css
--shadow-focus: 0 0 0 3px rgba(0, 82, 159, 0.4);
--shadow-focus-error: 0 0 0 3px rgba(211, 47, 47, 0.4);
```

### Elevation Usage

| Component | Shadow Token | Hover Shadow |
|-----------|--------------|--------------|
| Card (default) | `--shadow-sm` | `--shadow-md` |
| Button (elevated) | `--shadow-xs` | `--shadow-sm` |
| Dropdown | `--shadow-md` | - |
| Modal | `--shadow-xl` | - |
| Tooltip | `--shadow-md` | - |
| Navigation (sticky) | `--shadow-sm` | - |

---

## Components

### Buttons

#### Button Variants

| Variant | Background | Text | Border | Usage |
|---------|------------|------|--------|-------|
| **Primary** | `--color-blue-700` | `#FFFFFF` | none | Main CTA, form submissions |
| **Secondary** | `transparent` | `--color-blue-700` | `2px solid --color-blue-700` | Secondary actions |
| **Tertiary** | `transparent` | `--color-blue-700` | none | Low-priority actions |
| **Accent** | `--color-yellow-700` | `--color-gray-900` | none | Promotional CTAs |
| **Destructive** | `--color-error` | `#FFFFFF` | none | Delete, cancel actions |
| **Ghost** | `transparent` | `--color-gray-700` | none | Minimal actions |

#### Button Sizes

| Size | Height | Padding | Font Size | Icon Size |
|------|--------|---------|-----------|-----------|
| **Small** | 32px | 8px 16px | 14px | 16px |
| **Medium** | 40px | 12px 24px | 16px | 20px |
| **Large** | 48px | 16px 32px | 18px | 24px |

#### Button States

```css
.btn-primary {
  background-color: var(--color-blue-700);
  color: white;
  border: none;
  border-radius: var(--radius-md);
  font-weight: var(--font-weight-semibold);
  transition: all 0.2s ease;
}

.btn-primary:hover {
  background-color: var(--color-blue-800);
}

.btn-primary:active {
  background-color: var(--color-blue-900);
}

.btn-primary:focus-visible {
  outline: none;
  box-shadow: var(--shadow-focus);
}

.btn-primary:disabled {
  background-color: var(--color-gray-300);
  color: var(--color-gray-500);
  cursor: not-allowed;
}
```

### Form Elements

#### Text Fields

Text fields use Material Design Component (MDC) patterns with custom styling.

```css
.text-field {
  position: relative;
  margin-bottom: var(--spacing-6);
}

.text-field__input {
  width: 100%;
  height: 56px;
  padding: 16px;
  font-size: var(--text-body-md);
  font-family: var(--font-family-secondary);
  border: 1px solid var(--color-gray-400);
  border-radius: var(--radius-md);
  background-color: var(--color-white);
  transition: border-color 0.2s, box-shadow 0.2s;
}

.text-field__input:focus {
  outline: none;
  border-color: var(--color-blue-700);
  box-shadow: var(--shadow-focus);
}

.text-field__input:invalid,
.text-field__input--error {
  border-color: var(--color-error);
}

.text-field__input:disabled {
  background-color: var(--color-gray-100);
  color: var(--color-gray-500);
  cursor: not-allowed;
}

.text-field__label {
  position: absolute;
  top: 16px;
  left: 16px;
  font-size: var(--text-body-md);
  color: var(--color-gray-600);
  transition: all 0.2s ease;
  pointer-events: none;
}

.text-field__label--float-above {
  top: 8px;
  font-size: var(--text-caption);
  color: var(--color-blue-700);
}

.text-field__helper-text {
  margin-top: var(--spacing-1);
  font-size: var(--text-caption);
  color: var(--color-gray-600);
}

.text-field__helper-text--error {
  color: var(--color-error);
}
```

#### Select/Dropdown

```css
.select {
  position: relative;
  width: 100%;
}

.select__input {
  appearance: none;
  width: 100%;
  height: 56px;
  padding: 16px 48px 16px 16px;
  font-size: var(--text-body-md);
  border: 1px solid var(--color-gray-400);
  border-radius: var(--radius-md);
  background: var(--color-white) url('chevron-down.svg') no-repeat right 16px center;
  cursor: pointer;
}
```

#### Checkbox & Radio

```css
.checkbox,
.radio {
  display: flex;
  align-items: center;
  gap: var(--spacing-3);
  cursor: pointer;
}

.checkbox__input,
.radio__input {
  width: 20px;
  height: 20px;
  border: 2px solid var(--color-gray-400);
  border-radius: var(--radius-sm); /* checkbox */
  /* border-radius: 50%; for radio */
}

.checkbox__input:checked,
.radio__input:checked {
  background-color: var(--color-blue-700);
  border-color: var(--color-blue-700);
}
```

### Cards

```css
.card {
  background-color: var(--color-white);
  border-radius: var(--radius-lg);
  box-shadow: var(--shadow-sm);
  overflow: hidden;
  transition: box-shadow 0.2s ease, transform 0.2s ease;
}

.card:hover {
  box-shadow: var(--shadow-md);
  transform: translateY(-2px);
}

.card__image {
  width: 100%;
  aspect-ratio: 16 / 10;
  object-fit: cover;
}

.card__content {
  padding: var(--spacing-6);
}

.card__title {
  font-family: var(--font-family-primary);
  font-size: var(--text-h4);
  font-weight: var(--font-weight-semibold);
  color: var(--color-gray-900);
  margin-bottom: var(--spacing-2);
}

.card__description {
  font-family: var(--font-family-secondary);
  font-size: var(--text-body-sm);
  color: var(--color-gray-700);
  margin-bottom: var(--spacing-4);
}

.card__actions {
  display: flex;
  gap: var(--spacing-3);
  padding: var(--spacing-4) var(--spacing-6);
  border-top: 1px solid var(--color-gray-200);
}
```

### Navigation

```css
.navbar {
  position: sticky;
  top: 0;
  z-index: 1000;
  background-color: var(--color-white);
  box-shadow: var(--shadow-sm);
}

.navbar__container {
  display: flex;
  align-items: center;
  justify-content: space-between;
  max-width: 1200px;
  margin: 0 auto;
  padding: var(--spacing-4) var(--spacing-6);
}

.navbar__logo {
  height: 32px;
}

.navbar__nav {
  display: flex;
  gap: var(--spacing-8);
}

.navbar__link {
  font-family: var(--font-family-secondary);
  font-size: var(--text-body-md);
  font-weight: var(--font-weight-medium);
  color: var(--color-gray-800);
  text-decoration: none;
  transition: color 0.2s;
}

.navbar__link:hover {
  color: var(--color-blue-700);
}

.navbar__link--active {
  color: var(--color-blue-700);
  border-bottom: 2px solid var(--color-blue-700);
}
```

### Modals

```css
.modal-overlay {
  position: fixed;
  inset: 0;
  background-color: rgba(0, 0, 0, 0.5);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 2000;
}

.modal {
  background-color: var(--color-white);
  border-radius: var(--radius-lg);
  box-shadow: var(--shadow-xl);
  max-width: 560px;
  width: calc(100% - var(--spacing-8));
  max-height: 90vh;
  overflow-y: auto;
}

.modal__header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: var(--spacing-6);
  border-bottom: 1px solid var(--color-gray-200);
}

.modal__title {
  font-family: var(--font-family-primary);
  font-size: var(--text-h4);
  font-weight: var(--font-weight-semibold);
  color: var(--color-gray-900);
}

.modal__body {
  padding: var(--spacing-6);
}

.modal__footer {
  display: flex;
  justify-content: flex-end;
  gap: var(--spacing-3);
  padding: var(--spacing-6);
  border-top: 1px solid var(--color-gray-200);
}
```

### Alerts & Notifications

```css
.alert {
  display: flex;
  gap: var(--spacing-3);
  padding: var(--spacing-4);
  border-radius: var(--radius-md);
  font-family: var(--font-family-secondary);
  font-size: var(--text-body-sm);
}

.alert--info {
  background-color: var(--color-info-light);
  border-left: 4px solid var(--color-info);
  color: var(--color-gray-800);
}

.alert--success {
  background-color: var(--color-success-light);
  border-left: 4px solid var(--color-success);
}

.alert--warning {
  background-color: var(--color-warning-light);
  border-left: 4px solid var(--color-warning);
}

.alert--error {
  background-color: var(--color-error-light);
  border-left: 4px solid var(--color-error);
}
```

### Badges & Chips

```css
.badge {
  display: inline-flex;
  align-items: center;
  padding: var(--spacing-1) var(--spacing-3);
  font-size: var(--text-caption);
  font-weight: var(--font-weight-semibold);
  border-radius: var(--radius-full);
}

.badge--primary {
  background-color: var(--color-blue-700);
  color: white;
}

.badge--secondary {
  background-color: var(--color-gray-200);
  color: var(--color-gray-800);
}

.badge--accent {
  background-color: var(--color-yellow-700);
  color: var(--color-gray-900);
}

.chip {
  display: inline-flex;
  align-items: center;
  gap: var(--spacing-2);
  padding: var(--spacing-2) var(--spacing-4);
  font-size: var(--text-body-sm);
  background-color: var(--color-gray-100);
  border-radius: var(--radius-full);
  cursor: pointer;
}

.chip--selected {
  background-color: var(--color-blue-100);
  border: 1px solid var(--color-blue-700);
  color: var(--color-blue-700);
}
```

---

## Iconography

### Icon Sizes

| Size | Dimensions | Usage |
|------|------------|-------|
| **xs** | 12px | Inline indicators |
| **sm** | 16px | Small UI elements, badges |
| **md** | 20px | Buttons, form elements |
| **lg** | 24px | **Default**, navigation, actions |
| **xl** | 32px | Feature icons |
| **2xl** | 48px | Illustrations, hero elements |

### Icon Guidelines

- Use consistent stroke width (1.5px - 2px)
- Maintain 24px viewBox for scalability
- Use `currentColor` for fill/stroke to inherit text color
- Ensure 44x44px minimum touch target for interactive icons

---

## Accessibility

### WCAG 2.1 AA Compliance

The platform design system is built to meet WCAG 2.1 AA standards:

#### Color Contrast

| Text Type | Minimum Ratio | Implementation |
|-----------|---------------|----------------|
| Normal text (< 18px) | 4.5:1 | All text colors meet this requirement |
| Large text (≥ 18px bold or ≥ 24px) | 3:1 | Headlines and display text compliant |
| UI components | 3:1 | All interactive elements compliant |

#### Focus States

- All interactive elements MUST have visible focus indicators
- Focus ring color: `rgba(0, 82, 159, 0.4)` (3px outline)
- Never remove focus styles; only enhance them
- Tab order must follow logical reading order

#### Keyboard Navigation

- All functionality available via keyboard
- No tabindex values greater than 0
- Skip links provided for main content
- Modal focus trapping implemented

#### Screen Reader Support

- Semantic HTML elements required
- ARIA labels for non-text content
- Live regions for dynamic updates
- Proper heading hierarchy (h1 → h2 → h3)

---

## Design Tokens

### Complete Token Reference (CSS Custom Properties)

```css
:root {
  /* ==================== COLORS ==================== */

  /* Primary Brand */
  --color-primary: #00529F;
  --color-primary-dark: #003366;
  --color-primary-light: #0066CC;
  --color-accent: #FFD520;
  --color-accent-dark: #DAA520;

  /* Blue Scale */
  --color-blue-50: #F5F9FF;
  --color-blue-100: #E3F2FD;
  --color-blue-200: #BBDEFB;
  --color-blue-300: #90CAF9;
  --color-blue-400: #42A5F5;
  --color-blue-500: #1976D2;
  --color-blue-600: #0066CC;
  --color-blue-700: #00529F;
  --color-blue-800: #003366;
  --color-blue-900: #001F3F;

  /* Yellow Scale */
  --color-yellow-100: #FFFDE7;
  --color-yellow-200: #FFF8E1;
  --color-yellow-300: #FFF2CC;
  --color-yellow-400: #FFEB99;
  --color-yellow-500: #FFE566;
  --color-yellow-600: #FFDD4B;
  --color-yellow-700: #FFD520;
  --color-yellow-800: #DAA520;
  --color-yellow-900: #B8860B;

  /* Gray Scale */
  --color-gray-50: #FAFAFA;
  --color-gray-100: #F2F2F2;
  --color-gray-200: #E5E5E5;
  --color-gray-300: #CCCCCC;
  --color-gray-400: #999999;
  --color-gray-500: #808080;
  --color-gray-600: #666666;
  --color-gray-700: #4D4D4D;
  --color-gray-800: #333333;
  --color-gray-900: #1A1A1A;

  /* Semantic Colors */
  --color-success: #2E7D32;
  --color-success-light: #E8F5E9;
  --color-warning: #ED6C02;
  --color-warning-light: #FFF3E0;
  --color-error: #D32F2F;
  --color-error-light: #FFEBEE;
  --color-info: #0288D1;
  --color-info-light: #E1F5FE;

  /* Background Colors */
  --color-bg-primary: #FFFFFF;
  --color-bg-secondary: #FAFAFA;
  --color-bg-tertiary: #F2F2F2;

  /* Text Colors */
  --color-text-primary: #1A1A1A;
  --color-text-secondary: #4D4D4D;
  --color-text-tertiary: #666666;
  --color-text-disabled: #999999;
  --color-text-inverse: #FFFFFF;
  --color-text-link: #00529F;

  /* Border Colors */
  --color-border-default: #CCCCCC;
  --color-border-light: #E5E5E5;
  --color-border-focus: #00529F;
  --color-border-error: #D32F2F;

  /* ==================== TYPOGRAPHY ==================== */

  /* Font Families */
  --font-family-primary: 'Sharp Sans', 'Montserrat', -apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif;
  --font-family-secondary: 'Lato', 'Roboto', -apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif;
  --font-family-mono: 'SF Mono', 'Monaco', 'Inconsolata', 'Fira Mono', monospace;

  /* Font Weights */
  --font-weight-light: 300;
  --font-weight-regular: 400;
  --font-weight-medium: 500;
  --font-weight-semibold: 600;
  --font-weight-bold: 700;
  --font-weight-extrabold: 800;

  /* Font Sizes */
  --text-display-xl: 4.5rem;    /* 72px */
  --text-display-lg: 3.75rem;   /* 60px */
  --text-display-md: 3rem;      /* 48px */
  --text-display-sm: 2.25rem;   /* 36px */
  --text-h1: 2rem;              /* 32px */
  --text-h2: 1.75rem;           /* 28px */
  --text-h3: 1.5rem;            /* 24px */
  --text-h4: 1.25rem;           /* 20px */
  --text-h5: 1.125rem;          /* 18px */
  --text-h6: 1rem;              /* 16px */
  --text-body-lg: 1.125rem;     /* 18px */
  --text-body-md: 1rem;         /* 16px */
  --text-body-sm: 0.875rem;     /* 14px */
  --text-caption: 0.75rem;      /* 12px */
  --text-micro: 0.625rem;       /* 10px */

  /* Line Heights */
  --line-height-tight: 1.1;
  --line-height-snug: 1.25;
  --line-height-normal: 1.4;
  --line-height-relaxed: 1.6;
  --line-height-loose: 1.8;

  /* Letter Spacing */
  --letter-spacing-tighter: -0.02em;
  --letter-spacing-tight: -0.01em;
  --letter-spacing-normal: 0;
  --letter-spacing-wide: 0.01em;
  --letter-spacing-wider: 0.02em;
  --letter-spacing-widest: 0.1em;

  /* ==================== SPACING ==================== */

  --spacing-0: 0;
  --spacing-1: 0.25rem;   /* 4px */
  --spacing-2: 0.5rem;    /* 8px */
  --spacing-3: 0.75rem;   /* 12px */
  --spacing-4: 1rem;      /* 16px */
  --spacing-5: 1.25rem;   /* 20px */
  --spacing-6: 1.5rem;    /* 24px */
  --spacing-8: 2rem;      /* 32px */
  --spacing-10: 2.5rem;   /* 40px */
  --spacing-12: 3rem;     /* 48px */
  --spacing-16: 4rem;     /* 64px */
  --spacing-20: 5rem;     /* 80px */
  --spacing-24: 6rem;     /* 96px */
  --spacing-32: 8rem;     /* 128px */

  /* ==================== LAYOUT ==================== */

  /* Breakpoints (for reference, use in media queries) */
  --breakpoint-xs: 0;
  --breakpoint-sm: 576px;
  --breakpoint-md: 768px;
  --breakpoint-lg: 992px;
  --breakpoint-xl: 1200px;
  --breakpoint-xxl: 1400px;

  /* Container */
  --container-max-width: 1200px;
  --container-padding: var(--spacing-6);

  /* Grid */
  --grid-columns: 12;
  --grid-gutter: var(--spacing-6);

  /* ==================== BORDER RADIUS ==================== */

  --radius-none: 0;
  --radius-sm: 0.25rem;   /* 4px */
  --radius-md: 0.5rem;    /* 8px */
  --radius-lg: 0.75rem;   /* 12px */
  --radius-xl: 1rem;      /* 16px */
  --radius-2xl: 1.5rem;   /* 24px */
  --radius-full: 9999px;

  /* ==================== SHADOWS ==================== */

  --shadow-none: none;
  --shadow-xs: 0 1px 2px rgba(0, 0, 0, 0.05);
  --shadow-sm: 0 1px 3px rgba(0, 0, 0, 0.1), 0 1px 2px rgba(0, 0, 0, 0.06);
  --shadow-md: 0 4px 6px rgba(0, 0, 0, 0.1), 0 2px 4px rgba(0, 0, 0, 0.06);
  --shadow-lg: 0 10px 15px rgba(0, 0, 0, 0.1), 0 4px 6px rgba(0, 0, 0, 0.05);
  --shadow-xl: 0 20px 25px rgba(0, 0, 0, 0.1), 0 10px 10px rgba(0, 0, 0, 0.04);
  --shadow-2xl: 0 25px 50px rgba(0, 0, 0, 0.25);
  --shadow-focus: 0 0 0 3px rgba(0, 82, 159, 0.4);
  --shadow-focus-error: 0 0 0 3px rgba(211, 47, 47, 0.4);

  /* ==================== TRANSITIONS ==================== */

  --transition-fast: 150ms ease;
  --transition-normal: 200ms ease;
  --transition-slow: 300ms ease;
  --transition-slower: 500ms ease;

  /* ==================== Z-INDEX ==================== */

  --z-dropdown: 100;
  --z-sticky: 200;
  --z-fixed: 300;
  --z-modal-backdrop: 400;
  --z-modal: 500;
  --z-popover: 600;
  --z-tooltip: 700;
  --z-toast: 800;
}
```

---

## CSS Variables Reference

### Quick Reference Card

```css
/* Colors */
var(--color-primary)          /* #00529F - Main brand blue */
var(--color-accent)           /* #FFD520 - Brand yellow */
var(--color-gray-900)         /* #1A1A1A - Primary text */
var(--color-gray-700)         /* #4D4D4D - Secondary text */
var(--color-error)            /* #D32F2F - Error red */
var(--color-success)          /* #2E7D32 - Success green */

/* Typography */
var(--font-family-primary)    /* Sharp Sans / Montserrat */
var(--font-family-secondary)  /* Lato / Roboto */
var(--text-h1)                /* 2rem (32px) */
var(--text-body-md)           /* 1rem (16px) */
var(--font-weight-semibold)   /* 600 */

/* Spacing */
var(--spacing-2)              /* 8px - Base unit */
var(--spacing-4)              /* 16px - Standard */
var(--spacing-6)              /* 24px - Component padding */
var(--spacing-8)              /* 32px - Large gaps */

/* Layout */
var(--container-max-width)    /* 1200px */
var(--grid-gutter)            /* 24px */

/* Border Radius */
var(--radius-md)              /* 8px - Default */
var(--radius-lg)              /* 12px - Cards */
var(--radius-full)            /* Pills/Circles */

/* Shadows */
var(--shadow-sm)              /* Default card shadow */
var(--shadow-md)              /* Hover/elevated shadow */
var(--shadow-focus)           /* Focus ring */

/* Transitions */
var(--transition-normal)      /* 200ms ease */
```

---

## Version History

| Version | Date | Changes |
|---------|------|---------|
| 1.0.0 | January 2026 | Initial release |
