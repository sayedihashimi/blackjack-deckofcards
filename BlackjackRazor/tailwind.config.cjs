module.exports = {
  darkMode: 'class',
  content: [
    './Pages/**/*.{cshtml,html}',
    './**/*.cshtml'
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
