/** @type {import('tailwindcss').Config} */
export default {
    content: [
        "./index.html",
        "./src/**/*.{js,ts,jsx,tsx}",
    ],
    theme: {
        extend: {
            colors: {
                primary: {
                    DEFAULT: '#8B5CF6',
                    foreground: '#FFFFFF',
                },
                yellow: {
                    400: '#FBBF24',
                },
                orange: {
                    500: '#F59E0B',
                }
            },
            backgroundImage: {
                'primary-gradient': 'linear-gradient(135deg, #6366f1 0%, #8b5cf6 100%)',
                'ai-badge-gradient': 'linear-gradient(135deg, #FBBF24 0%, #F59E0B 100%)',
            }
        },
    },
    plugins: [],
}