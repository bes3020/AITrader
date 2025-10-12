# Color Themes

The Trading Strategy Analyzer includes 6 carefully designed color themes to suit different preferences and use cases.

## Available Themes

### 1. **Default** (Slate Gray)
Clean and professional slate gray theme with traditional green/red profit/loss indicators.
- **Best for:** General use, traditional finance look
- **Primary:** Dark slate gray
- **Profit:** Vibrant green
- **Loss:** Vibrant red

### 2. **Ocean Blue** (Financial Terminal)
Deep blue theme inspired by professional financial terminals like Bloomberg.
- **Best for:** Traders who prefer blue interfaces
- **Primary:** Deep ocean blue
- **Profit:** Teal green
- **Loss:** Coral red
- **Feel:** Professional, calming

### 3. **Forest Green** (Earthy)
Earthy green theme for a calming, nature-inspired experience.
- **Best for:** Reducing eye strain, extended use
- **Primary:** Forest green
- **Profit:** Bright green
- **Loss:** Burnt orange
- **Feel:** Natural, relaxing

### 4. **Sunset Orange** (Energetic)
Warm orange and purple theme for an energetic, modern feel.
- **Best for:** Creative users, modern aesthetic
- **Primary:** Sunset orange
- **Profit:** Lime green
- **Loss:** Crimson red
- **Feel:** Warm, energetic

### 5. **Midnight Purple** (Sophisticated)
Deep purple theme for a sophisticated, premium look.
- **Best for:** Dark mode enthusiasts, luxury feel
- **Primary:** Royal purple
- **Profit:** Mint green
- **Loss:** Rose red
- **Feel:** Elegant, premium

### 6. **Terminal Monochrome** (Classic)
Classic terminal green-on-black aesthetic for retro computing fans.
- **Best for:** Developers, terminal enthusiasts
- **Primary:** Terminal green
- **Profit:** Bright terminal green
- **Loss:** Terminal red
- **Feel:** Nostalgic, focused

## How to Use

### Switching Themes
1. Click the **palette icon** (🎨) in the top-right corner of any page
2. Browse available themes with color previews
3. Click on a theme to apply it instantly
4. Your selection is saved automatically in browser storage

### Theme Persistence
- Themes are saved in `localStorage` and persist across sessions
- Each theme automatically adapts to light/dark mode based on system preferences
- No account required - themes are saved per browser

### Customizing Themes

To add or modify themes, edit `/lib/themes.ts`:

```typescript
{
  name: "custom",
  label: "Custom Theme",
  description: "Your custom theme description",
  light: {
    background: "oklch(98% 0 0)",
    foreground: "oklch(15% 0 0)",
    // ... more colors
  },
  dark: {
    // Dark mode colors
  }
}
```

## Color Format

All colors use the **OKLCH** color space for:
- **Perceptual uniformity:** Colors that look equally bright
- **Better dark modes:** More natural color transitions
- **Future-proof:** Part of CSS Color Level 4 specification

Format: `oklch(lightness% chroma hue)`
- **Lightness:** 0-100% (brightness)
- **Chroma:** 0-0.4 (saturation/intensity)
- **Hue:** 0-360° (color angle)

## Browser Support

Themes work in all modern browsers with CSS Custom Properties support:
- Chrome/Edge 88+
- Firefox 86+
- Safari 15+

Fallback to default theme in older browsers.

## Accessibility

All themes maintain:
- **WCAG AA contrast ratios** for text readability
- **Distinguishable profit/loss colors** for colorblind users
- **Consistent visual hierarchy** across themes
- **Focus indicators** that work on all backgrounds

## Tips

- **Dark mode users:** Try Midnight Purple or Terminal for best experience
- **Light mode users:** Ocean Blue and Default work great
- **Long sessions:** Forest Green reduces eye strain
- **High energy:** Sunset Orange for active trading days
- **Professional screenshots:** Default or Ocean Blue look most traditional
