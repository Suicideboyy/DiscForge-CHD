# DiscForge CHD design system — 2.6.0

Source: ui-ux-pro-max-cli 2.15.0, https://github.com/nextlevelbuilder/ui-ux-pro-max-skill (MIT).
Catalog searches: gaming library dashboard; explicit style: bento grid dashboard; stack: winui desktop keyboard theme accessibility.

The generic design-system search returned a showcase/3D direction. That landing-page pattern does not fit a native converter. The verified Bento Box Grid style is the selected low-cost desktop adaptation; the gaming palette supplies its colors.

- Canvas #0F0F23; cards #1E1C35; inset #27273B; border #3F3D56.
- Text #E2E8F0; muted #94A3B8; accent #A78BFA; primary action #7C3AED.
- Status and metric accents: teal #5EEAD4, amber #FBBF24; stop action has a distinct red surface.
- Native Segoe UI, 12–15 px body, 17 px sections, 27 px brand; no downloaded fonts.
- 16 px card corners, 16–24 px spacing; two columns stack at existing responsive breakpoints.
- Keep native input states, keyboard navigation, help and focus indicators. Start access key: Alt+S.
- Use native entrance transitions only when Windows animations are enabled; no 3D, parallax, scaling buttons or JavaScript animation libraries.
- WebView2 uses the same palette and supports forced-colors. Cover assets retain their original appearance.
- No CLI, Node.js or Python runtime is shipped in the application. The design catalogs are development guidance only.
