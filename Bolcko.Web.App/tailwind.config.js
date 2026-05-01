/** @type {import('tailwindcss').Config} */
module.exports = {
    darkMode: "class",

    content: [
        "./Views/**/*.cshtml",
        "./Pages/**/*.cshtml",
        "./Areas/**/*.cshtml",
        "./wwwroot/**/*.html",
        "./wwwroot/**/*.js"
    ],

    theme: {
        extend: {
            colors: {
                primary: "#815500",
                "primary-container": "#e8a020",
                "primary-fixed": "#ffddb2",
                "primary-fixed-dim": "#ffb94c",
                "on-primary": "#ffffff",
                "on-primary-container": "#5b3b00",
                "on-primary-fixed": "#291800",
                "on-primary-fixed-variant": "#624000",

                secondary: "#575e74",
                "secondary-container": "#dbe2fc",
                "secondary-fixed": "#dbe2fc",
                "secondary-fixed-dim": "#bfc6df",
                "on-secondary": "#ffffff",
                "on-secondary-container": "#5d647a",
                "on-secondary-fixed": "#141b2e",
                "on-secondary-fixed-variant": "#3f465b",

                tertiary: "#545f70",
                "tertiary-container": "#a4afc3",
                "tertiary-fixed": "#d8e3f8",
                "tertiary-fixed-dim": "#bcc7db",
                "on-tertiary": "#ffffff",
                "on-tertiary-container": "#384253",
                "on-tertiary-fixed": "#111c2b",
                "on-tertiary-fixed-variant": "#3d4758",

                background: "#faf8ff",
                surface: "#faf8ff",
                "surface-dim": "#d5d9eb",
                "surface-bright": "#faf8ff",
                "surface-container": "#e9edff",
                "surface-container-low": "#f1f3ff",
                "surface-container-high": "#e4e7f9",
                "surface-container-highest": "#dee2f3",
                "surface-container-lowest": "#ffffff",
                "surface-variant": "#dee2f3",
                "surface-tint": "#815500",

                "on-background": "#161b28",
                "on-surface": "#161b28",
                "on-surface-variant": "#514534",
                "inverse-surface": "#2b303d",
                "inverse-on-surface": "#edf0ff",
                "inverse-primary": "#ffb94c",

                outline: "#847562",
                "outline-variant": "#d6c4ae",

                error: "#ba1a1a",
                "error-container": "#ffdad6",
                "on-error": "#ffffff",
                "on-error-container": "#93000a"
            },

            boxShadow: {
                hard: "2px 2px 0px 0px rgba(27, 34, 53, 0.15)"
            },

            borderRadius: {
                DEFAULT: "0.125rem",
                sm: "0.125rem",
                md: "0.25rem",
                lg: "0.25rem",
                xl: "0.5rem",
                full: "9999px" 
            },

            spacing: {
                xs: "4px",
                base: "8px",
                sm: "12px",
                md: "24px",
                lg: "48px",
                xl: "80px",
                gutter: "24px",
                margin: "32px"
            },

            fontFamily: {
                body: ["Noto Kufi Arabic", "Inter", "sans-serif"],
                headline: ["Noto Kufi Arabic", "Work Sans", "sans-serif"],
                label: ["Noto Kufi Arabic", "Inter", "sans-serif"]
            },

            fontSize: {
                "label-sm": ["12px", { lineHeight: "1rem", fontWeight: "500" }],
                "label-bold": ["14px", { lineHeight: "1rem", fontWeight: "700" }],
                "body-md": ["16px", { lineHeight: "1.5rem", fontWeight: "400" }],
                "body-lg": ["18px", { lineHeight: "1.6rem", fontWeight: "400" }],
                "headline-md": ["24px", { lineHeight: "1.3", fontWeight: "600" }],
                "headline-lg": ["32px", { lineHeight: "1.2", fontWeight: "600", letterSpacing: "-0.01em" }],
                "headline-xl": ["48px", { lineHeight: "1.1", fontWeight: "700", letterSpacing: "-0.02em" }]
            }
        }
    },

    plugins: []
};