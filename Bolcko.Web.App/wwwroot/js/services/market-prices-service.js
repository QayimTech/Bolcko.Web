/**
 * BLOCKO Market Prices Service (SOLID - Single Responsibility Principle)
 * Responsible ONLY for communicating with the Market Prices endpoints.
 * Utilizes the low-level ApiClient dependency.
 */
(function (window) {
    'use strict';

    class MarketPricesService {
        constructor(apiClient) {
            this.apiClient = apiClient || window.ApiClient;
        }

        /**
         * Fetches the formatted HTML block for market prices asynchronously
         * @param {string} endpointUrl - The endpoint to fetch market prices from
         * @returns {Promise<string>} The rendered HTML content
         */
        async fetchPricesHtml(endpointUrl) {
            if (!this.apiClient) {
                throw new Error("API Client dependency is not initialized.");
            }
            return await this.apiClient.get(endpointUrl);
        }
    }

    // Export instances globally to be consumed by UI views
    window.MarketPricesService = new MarketPricesService();

})(window);
