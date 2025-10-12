# Trading Strategy Frontend

A Next.js 14 TypeScript application for analyzing and backtesting trading strategies.

## Getting Started

### Prerequisites

- Node.js 18+
- npm or yarn

### Installation

```bash
npm install
```

### Environment Variables

Copy `.env.example` to `.env.local` and configure:

```
NEXT_PUBLIC_API_URL=http://localhost:5000
```

### Development

```bash
npm run dev
```

Open [http://localhost:3000](http://localhost:3000) with your browser.

### Build

```bash
npm run build
npm start
```

## Project Structure

```
app/
├── page.tsx                    # Home page - strategy input
├── results/[id]/page.tsx       # Results page for specific strategy
├── layout.tsx                  # Root layout
├── globals.css                 # Global styles
└── api/strategy/route.ts       # Optional API route

components/
├── StrategyForm.tsx            # Strategy input form
├── ResultsSummary.tsx          # Results summary with metrics
├── WorstTradesSection.tsx      # Section for worst trades
├── TradeCard.tsx               # Individual trade card
├── TradeChart.tsx              # Trading chart using lightweight-charts
└── ui/                         # shadcn/ui components
    ├── button.tsx
    ├── card.tsx
    ├── input.tsx
    ├── label.tsx
    ├── badge.tsx
    └── select.tsx

lib/
├── api-client.ts               # API client for backend communication
├── utils.ts                    # Utility functions
└── types.ts                    # TypeScript type definitions
```

## Tech Stack

- **Framework:** Next.js 14 (App Router)
- **Language:** TypeScript (strict mode)
- **Styling:** Tailwind CSS
- **UI Components:** Radix UI (shadcn/ui)
- **Charts:** lightweight-charts
- **HTTP Client:** Axios
- **Date Utilities:** date-fns
- **Validation:** Zod

## Custom Colors

- **Profit:** Green (`hsl(142, 76%, 36%)`)
- **Loss:** Red (`hsl(0, 84%, 60%)`)

## TODO

Files are created with placeholder comments. Implement:

1. **StrategyForm.tsx** - Strategy input with conditions
2. **ResultsSummary.tsx** - Display metrics and summary
3. **WorstTradesSection.tsx** - Show worst performing trades
4. **TradeCard.tsx** - Individual trade details
5. **TradeChart.tsx** - Interactive price chart
6. **Home page** - Integrate StrategyForm
7. **Results page** - Display full strategy results
