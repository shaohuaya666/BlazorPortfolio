/** @type {import('tailwindcss').Config} */
module.exports = {
  content: [
    './**/*.razor',
    './**/*.cshtml',
    './**/*.html',
  ],
  darkMode: 'class',
  theme: {
    extend: {
      fontFamily: {
        sans: ['Inter', 'sans-serif'],
        mono: ['JetBrains Mono', 'monospace'],
        geist: ['Geist', 'sans-serif'],
      },
    },
  },
  plugins: [],
}
