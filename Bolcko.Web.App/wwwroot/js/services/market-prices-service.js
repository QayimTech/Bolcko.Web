/**
 * BLOCKO Market Prices Service (SOLID - Single Responsibility Principle)
 * Responsible ONLY for communicating with the Market Prices endpoints.
 * Uses browser's native fetch for simplicity.
 */
(function (window) {
    'use strict';

    class MarketPricesService {
        constructor() {}

        /**
         * Fetches the formatted HTML block for market prices asynchronously
         * @param {string} endpointUrl - The endpoint to fetch market prices from
         * @returns {Promise<string>} The rendered HTML content
         */
        async fetchPricesHtml(endpointUrl) {
            const response = await fetch(endpointUrl);
            if (!response.ok) {
                throw new Error(`HTTP error! Status: ${response.status}`);
            }
            return await response.text();
        }
    }

    // Export instances globally to be consumed by UI views and freeze to protect integrity
    window.MarketPricesService = Object.freeze(new MarketPricesService());

})(window);
