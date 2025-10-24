module.exports = {
  darkMode: 'class',
  content: [
    './Pages/**/*.{cshtml,html}',
    './wwwroot/css/site.css',
  ],
  theme: {
    extend: {
      colors: {
        casinoGreen: {
          950: '#102a13',
          900: '#14532d',
        },
        gold: {
          400: '#FFD700',
          500: '#FFC300',
        },
      },
      boxShadow: {
        casino: '0 0 20px 2px #FFD700',
      },
      borderRadius: {
        casino: '1.5rem',
      },
    },
  },
  plugins: [],
};