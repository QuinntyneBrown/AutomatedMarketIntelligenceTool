# Style Guide Compliance Requirements

## Automated Market Intelligence Tool

**Document Version:** 1.0.0
**Effective Date:** January 2026
**Status:** MANDATORY

---

## 1. Purpose

This document establishes the mandatory requirements and acceptance criteria for all UI/UX development within the Automated Market Intelligence Tool. All new components, features, and modifications MUST strictly adhere to the [Platform Style Guide](./style-guide.md) to ensure brand consistency, accessibility compliance, and maintainable code.

---

## 2. Scope

These requirements apply to:

- All new UI components
- All modifications to existing components
- All new pages and views
- All responsive design implementations
- All accessibility implementations
- All theming and styling code
- All design system token usage
- All third-party component integrations

---

## 3. Compliance Requirements

### 3.1 Color System Compliance

#### REQ-COLOR-001: Primary Brand Colors
**Priority:** CRITICAL
**Description:** All primary actions and brand elements MUST use the official platform color palette.

**Acceptance Criteria:**
- [ ] Primary blue (`#00529F`) is used for main CTAs, links, and primary brand elements
- [ ] Accent yellow (`#FFD520`) is used only for promotional highlights and secondary CTAs
- [ ] No custom colors are introduced without design team approval
- [ ] All color values reference design tokens, not hardcoded hex values

#### REQ-COLOR-002: Extended Color Palette
**Priority:** HIGH
**Description:** Extended color scales MUST be used from the defined palette.

**Acceptance Criteria:**
- [ ] Blue shades (50-900) are used for hover states, backgrounds, and variations
- [ ] Gray shades (50-900) are used for text, borders, and neutral elements
- [ ] Yellow shades are used sparingly and only for accent purposes
- [ ] No color interpolation or custom shades outside the defined scale

#### REQ-COLOR-003: Semantic Colors
**Priority:** HIGH
**Description:** Semantic colors MUST be used for their intended purposes.

**Acceptance Criteria:**
- [ ] Success states use `--color-success` (#2E7D32)
- [ ] Warning states use `--color-warning` (#ED6C02)
- [ ] Error states use `--color-error` (#D32F2F)
- [ ] Info states use `--color-info` (#0288D1)
- [ ] Light variants are used for backgrounds of semantic elements

#### REQ-COLOR-004: Color Contrast
**Priority:** CRITICAL
**Description:** All color combinations MUST meet WCAG 2.1 AA contrast requirements.

**Acceptance Criteria:**
- [ ] Normal text (< 18px) has minimum 4.5:1 contrast ratio
- [ ] Large text (≥ 18px bold or ≥ 24px) has minimum 3:1 contrast ratio
- [ ] UI components and graphical objects have minimum 3:1 contrast ratio
- [ ] Contrast is verified using automated testing tools
- [ ] No text is placed on backgrounds that fail contrast requirements

---

### 3.2 Typography Compliance

#### REQ-TYPO-001: Font Families
**Priority:** CRITICAL
**Description:** Only approved font families MUST be used.

**Acceptance Criteria:**
- [ ] Primary font (Sharp Sans/Montserrat) is used for headlines and display text
- [ ] Secondary font (Lato/Roboto) is used for body text and UI elements
- [ ] Monospace font is used only for code blocks and technical content
- [ ] System font fallbacks are properly configured
- [ ] No unapproved fonts are loaded or referenced

#### REQ-TYPO-002: Type Scale
**Priority:** HIGH
**Description:** Font sizes MUST follow the defined type scale.

**Acceptance Criteria:**
- [ ] H1 headings use `--text-h1` (32px/2rem)
- [ ] H2 headings use `--text-h2` (28px/1.75rem)
- [ ] H3 headings use `--text-h3` (24px/1.5rem)
- [ ] H4 headings use `--text-h4` (20px/1.25rem)
- [ ] Body text uses `--text-body-md` (16px/1rem)
- [ ] Small text uses `--text-body-sm` (14px/0.875rem)
- [ ] Caption text uses `--text-caption` (12px/0.75rem)
- [ ] No arbitrary font sizes outside the defined scale

#### REQ-TYPO-003: Font Weights
**Priority:** MEDIUM
**Description:** Font weights MUST be used consistently.

**Acceptance Criteria:**
- [ ] Headlines use `--font-weight-bold` (700) or `--font-weight-semibold` (600)
- [ ] Body text uses `--font-weight-regular` (400)
- [ ] Buttons and labels use `--font-weight-semibold` (600)
- [ ] Emphasis uses `--font-weight-medium` (500)
- [ ] Light weight (300) is used only for large display text

#### REQ-TYPO-004: Line Height and Spacing
**Priority:** MEDIUM
**Description:** Line heights and letter spacing MUST follow guidelines.

**Acceptance Criteria:**
- [ ] Headlines use tight line height (1.1-1.25)
- [ ] Body text uses relaxed line height (1.5-1.6)
- [ ] Letter spacing follows the defined scale
- [ ] Text blocks maintain comfortable reading width (50-75 characters)

---

### 3.3 Spacing Compliance

#### REQ-SPACE-001: Spacing Scale
**Priority:** HIGH
**Description:** All spacing MUST use the 8px-based spacing scale.

**Acceptance Criteria:**
- [ ] Spacing values reference design tokens (--spacing-1 through --spacing-32)
- [ ] No arbitrary pixel values for margins or padding
- [ ] Base unit (8px) is consistently applied
- [ ] Spacing creates visual rhythm and hierarchy

#### REQ-SPACE-002: Component Spacing
**Priority:** HIGH
**Description:** Components MUST follow defined spacing patterns.

**Acceptance Criteria:**
- [ ] Buttons use correct padding based on size variant
- [ ] Cards use `--spacing-6` (24px) internal padding
- [ ] Form fields have `--spacing-6` (24px) bottom margin
- [ ] Sections use `--spacing-16` (64px) vertical padding
- [ ] Related elements are grouped with `--spacing-4` (16px) gaps

#### REQ-SPACE-003: Layout Spacing
**Priority:** MEDIUM
**Description:** Page-level spacing MUST be consistent.

**Acceptance Criteria:**
- [ ] Container padding is `--spacing-6` (24px)
- [ ] Section spacing follows the defined patterns
- [ ] Component separation uses appropriate spacing tokens
- [ ] White space is used intentionally for visual hierarchy

---

### 3.4 Layout and Grid Compliance

#### REQ-LAYOUT-001: Responsive Breakpoints
**Priority:** CRITICAL
**Description:** Responsive designs MUST use defined breakpoints.

**Acceptance Criteria:**
- [ ] Mobile breakpoint (< 576px) is properly handled
- [ ] Tablet breakpoint (768px-991px) is properly handled
- [ ] Desktop breakpoint (≥ 992px) is properly handled
- [ ] Large desktop (≥ 1200px) caps content at 1200px max-width
- [ ] No custom breakpoints without design approval

#### REQ-LAYOUT-002: Grid System
**Priority:** HIGH
**Description:** Layouts MUST use the 12-column grid system.

**Acceptance Criteria:**
- [ ] Grid uses 12 columns on desktop
- [ ] Grid uses 8 columns on tablet
- [ ] Grid uses 4 columns on mobile
- [ ] Gutters use `--spacing-6` (24px) on desktop
- [ ] Gutters use `--spacing-4` (16px) on mobile

#### REQ-LAYOUT-003: Container Width
**Priority:** HIGH
**Description:** Content containers MUST respect maximum width.

**Acceptance Criteria:**
- [ ] Maximum content width is 1200px
- [ ] Containers are horizontally centered
- [ ] Full-width elements are intentional and documented
- [ ] Container padding is applied consistently

---

### 3.5 Border Radius Compliance

#### REQ-RADIUS-001: Radius Scale
**Priority:** MEDIUM
**Description:** Border radius MUST use the defined scale.

**Acceptance Criteria:**
- [ ] Buttons use `--radius-md` (8px)
- [ ] Input fields use `--radius-md` (8px)
- [ ] Cards use `--radius-lg` (12px)
- [ ] Modals use `--radius-lg` (12px)
- [ ] Chips and badges use `--radius-full` (pill shape)
- [ ] No arbitrary border-radius values

---

### 3.6 Shadow and Elevation Compliance

#### REQ-SHADOW-001: Shadow Scale
**Priority:** MEDIUM
**Description:** Shadows MUST use the defined elevation scale.

**Acceptance Criteria:**
- [ ] Cards use `--shadow-sm` by default
- [ ] Hover states use `--shadow-md`
- [ ] Dropdowns and popovers use `--shadow-md`
- [ ] Modals use `--shadow-xl`
- [ ] Focus states use `--shadow-focus`
- [ ] No custom box-shadow values

---

### 3.7 Component Compliance

#### REQ-COMP-001: Button Standards
**Priority:** CRITICAL
**Description:** Buttons MUST follow defined patterns.

**Acceptance Criteria:**
- [ ] Primary buttons use blue background with white text
- [ ] Secondary buttons use transparent background with blue border
- [ ] Button sizes follow sm/md/lg specifications
- [ ] All states are implemented (default, hover, active, focus, disabled)
- [ ] Focus states have visible focus ring
- [ ] Minimum touch target is 44x44px on mobile

#### REQ-COMP-002: Form Field Standards
**Priority:** CRITICAL
**Description:** Form elements MUST follow defined patterns.

**Acceptance Criteria:**
- [ ] Text fields are 56px height with 16px padding
- [ ] Labels use floating label pattern when applicable
- [ ] Error states show red border and error message
- [ ] Helper text is positioned below the field
- [ ] Required/optional indicators are properly displayed
- [ ] Focus states have visible focus ring

#### REQ-COMP-003: Card Standards
**Priority:** HIGH
**Description:** Cards MUST follow defined patterns.

**Acceptance Criteria:**
- [ ] Cards use white background with `--radius-lg`
- [ ] Cards have `--shadow-sm` by default
- [ ] Hover state elevates to `--shadow-md`
- [ ] Content padding is `--spacing-6` (24px)
- [ ] Action areas have top border separator

#### REQ-COMP-004: Navigation Standards
**Priority:** HIGH
**Description:** Navigation MUST follow defined patterns.

**Acceptance Criteria:**
- [ ] Navbar is sticky with white background
- [ ] Navbar has subtle shadow when scrolled
- [ ] Active states are visually distinct
- [ ] Mobile navigation follows hamburger menu pattern
- [ ] Navigation links have proper hover/focus states

#### REQ-COMP-005: Modal Standards
**Priority:** HIGH
**Description:** Modals MUST follow defined patterns.

**Acceptance Criteria:**
- [ ] Modal overlay uses semi-transparent black
- [ ] Modal container uses `--radius-lg` and `--shadow-xl`
- [ ] Modal has header, body, and footer sections
- [ ] Close button is accessible
- [ ] Focus is trapped within modal
- [ ] ESC key closes modal

---

### 3.8 Accessibility Compliance

#### REQ-A11Y-001: WCAG 2.1 AA Compliance
**Priority:** CRITICAL
**Description:** All components MUST meet WCAG 2.1 AA standards.

**Acceptance Criteria:**
- [ ] All functionality is keyboard accessible
- [ ] No keyboard traps exist
- [ ] Focus order is logical
- [ ] Skip links are provided
- [ ] ARIA labels are properly implemented
- [ ] Screen reader testing is completed

#### REQ-A11Y-002: Focus Management
**Priority:** CRITICAL
**Description:** Focus states MUST be clearly visible.

**Acceptance Criteria:**
- [ ] All interactive elements have visible focus indicators
- [ ] Focus ring uses `--shadow-focus` style
- [ ] Focus is never removed (only enhanced)
- [ ] Tab order follows logical reading order
- [ ] No tabindex values greater than 0

#### REQ-A11Y-003: Color Independence
**Priority:** HIGH
**Description:** Information MUST NOT rely solely on color.

**Acceptance Criteria:**
- [ ] Error states include icon and/or text in addition to color
- [ ] Links are distinguishable without relying on color alone
- [ ] Charts and graphs use patterns in addition to color
- [ ] Status indicators include non-color cues

#### REQ-A11Y-004: Semantic HTML
**Priority:** HIGH
**Description:** Semantic HTML elements MUST be used correctly.

**Acceptance Criteria:**
- [ ] Headings follow proper hierarchy (h1 → h2 → h3)
- [ ] Lists use appropriate list elements
- [ ] Buttons use `<button>` elements (not divs/spans)
- [ ] Links use `<a>` elements with href attributes
- [ ] Form elements have associated labels

---

### 3.9 Design Token Usage

#### REQ-TOKEN-001: CSS Variable Usage
**Priority:** CRITICAL
**Description:** All styling MUST use CSS custom properties (design tokens).

**Acceptance Criteria:**
- [ ] Colors reference `--color-*` tokens
- [ ] Spacing references `--spacing-*` tokens
- [ ] Typography references `--text-*` and `--font-*` tokens
- [ ] Border radius references `--radius-*` tokens
- [ ] Shadows reference `--shadow-*` tokens
- [ ] No hardcoded values that duplicate tokens

#### REQ-TOKEN-002: Token Modification
**Priority:** HIGH
**Description:** Design tokens MUST NOT be overridden locally.

**Acceptance Criteria:**
- [ ] Global tokens are modified only in theme files
- [ ] Component-specific tokens extend (not replace) global tokens
- [ ] Token changes go through design review process
- [ ] Breaking token changes are versioned

---

## 4. Code Review Checklist

Before approving any PR that includes UI changes, reviewers MUST verify:

### Visual Compliance
- [ ] Colors match the style guide palette
- [ ] Typography follows the type scale
- [ ] Spacing uses the spacing scale
- [ ] Border radius follows guidelines
- [ ] Shadows follow elevation guidelines

### Component Compliance
- [ ] Components follow established patterns
- [ ] All interaction states are implemented
- [ ] Hover, focus, active, disabled states work correctly
- [ ] Mobile responsive behavior is correct

### Accessibility Compliance
- [ ] Keyboard navigation works
- [ ] Focus states are visible
- [ ] Color contrast meets requirements
- [ ] ARIA attributes are correct
- [ ] Screen reader testing passed

### Token Usage
- [ ] CSS custom properties are used
- [ ] No hardcoded color/spacing values
- [ ] Tokens are used appropriately

---

## 5. Testing Requirements

### 5.1 Visual Regression Testing
All UI changes MUST pass visual regression tests:
- [ ] Component screenshots match approved designs
- [ ] Responsive layouts render correctly at all breakpoints
- [ ] Dark mode (if applicable) renders correctly

### 5.2 Accessibility Testing
All UI changes MUST pass accessibility tests:
- [ ] axe-core automated testing (0 violations)
- [ ] Keyboard navigation manual testing
- [ ] Screen reader testing (NVDA/VoiceOver)
- [ ] Color contrast verification

### 5.3 Cross-Browser Testing
All UI changes MUST be tested on:
- [ ] Chrome (latest 2 versions)
- [ ] Firefox (latest 2 versions)
- [ ] Safari (latest 2 versions)
- [ ] Edge (latest 2 versions)
- [ ] iOS Safari (latest 2 versions)
- [ ] Chrome for Android (latest 2 versions)

---

## 6. Exception Process

### 6.1 Requesting Exceptions
If a requirement cannot be met, the following process MUST be followed:

1. Document the specific requirement that cannot be met
2. Explain the technical or business reason for the exception
3. Propose an alternative solution
4. Get written approval from:
   - Design Lead
   - Tech Lead
   - Product Owner (for customer-facing changes)

### 6.2 Exception Documentation
All approved exceptions MUST be:
- Documented in the component's README
- Added to the exceptions registry
- Reviewed quarterly for removal

---

## 7. Enforcement

### 7.1 Automated Enforcement
The following automated checks are REQUIRED:
- Stylelint rules for design token usage
- ESLint rules for accessibility
- CI/CD pipeline visual regression tests
- Automated accessibility scanning

### 7.2 Manual Enforcement
Code reviewers are REQUIRED to:
- Verify visual compliance against style guide
- Test keyboard navigation
- Review accessibility implementation
- Reject PRs that violate requirements

### 7.3 Non-Compliance Consequences
Components that do not meet requirements:
- Will NOT be merged to main branch
- Will NOT be deployed to production
- Must be fixed before release
- May require design review meeting

---

## 8. Version History

| Version | Date | Author | Changes |
|---------|------|--------|---------|
| 1.0.0 | January 2026 | Platform Team | Initial release |

---

## 9. Approval

This document has been reviewed and approved by:

| Role | Name | Date | Signature |
|------|------|------|-----------|
| Design Lead | _______________ | _______________ | _______________ |
| Tech Lead | _______________ | _______________ | _______________ |
| Product Owner | _______________ | _______________ | _______________ |

---

## 10. References

- [Platform Style Guide](./style-guide.md)
- [WCAG 2.1 Guidelines](https://www.w3.org/WAI/WCAG21/quickref/)
