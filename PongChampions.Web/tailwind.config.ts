import type { Config } from 'tailwindcss';

function token(name: string) {
  return `hsl(var(--${name}) / <alpha-value>)`;
}

export default {
  content: ['./index.html', './src/**/*.{ts,tsx}'],
  darkMode: 'class',
  theme: {
    extend: {
      colors: {
        background: token('background'),
        foreground: token('foreground'),
        card: {
          DEFAULT: token('card'),
          foreground: token('card-foreground'),
        },
        primary: {
          DEFAULT: token('primary'),
          foreground: token('primary-foreground'),
        },
        secondary: {
          DEFAULT: token('secondary'),
          foreground: token('secondary-foreground'),
        },
        muted: {
          DEFAULT: token('muted'),
          foreground: token('muted-foreground'),
        },
        accent: {
          DEFAULT: token('accent'),
          foreground: token('accent-foreground'),
        },
        destructive: {
          DEFAULT: token('destructive'),
          foreground: token('destructive-foreground'),
        },
        success: token('success'),
        border: token('border'),
        input: token('input'),
        ring: token('ring'),
        court: {
          DEFAULT: token('court'),
          foreground: token('court-foreground'),
        },
        host: token('host'),
        guest: token('guest'),
      },
      spacing: {
        'safe-t': 'env(safe-area-inset-top)',
        'safe-b': 'env(safe-area-inset-bottom)',
        'safe-s': 'env(safe-area-inset-left)',
        'safe-e': 'env(safe-area-inset-right)',
      },
      fontSize: {
        display: ['clamp(2.25rem, 1.5rem + 3.5vw, 4rem)', { lineHeight: '1.05' }],
      },
      aspectRatio: {
        court: '16 / 10',
      },
    },
  },
} satisfies Config;
