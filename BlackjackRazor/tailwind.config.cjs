module.exports = {
  darkMode: 'class',
  content: [
    './Pages/**/*.{cshtml,html}',
    './**/*.cshtml'
  ],
  safelist: [
    // Button colors and states
    'bg-blue-600','bg-indigo-600','bg-green-600','bg-yellow-600','bg-pink-600','bg-orange-600','bg-red-600','bg-teal-600',
    'hover:brightness-110','active:brightness-95','ring-2','ring-yellow-400',
    // Panel backgrounds with opacity forms
    'bg-neutral-800/60','bg-neutral-800/70','text-neutral-300','text-neutral-400','text-neutral-700',
    // Card related
    'shadow','shadow-lg','shadow-xl',
    // Flex sizing
    'flex-[2]'
  ],
  theme: {
    extend: {
      colors: {
        table: { felt: '#0f3d2e', edge: '#062017' },
        accent: { win: '#16a34a', lose: '#dc2626', push: '#fbbf24' }
      },
      boxShadow: { card: '0 4px 10px rgba(0,0,0,0.45)' }
    }
  },
  plugins: []
};
