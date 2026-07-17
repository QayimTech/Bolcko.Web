/**
 * BLOCKO Centralized API Client (Fetch Wrapper)
 * Designed for Razor Views to solve CSRF, request deduplication (Race Conditions),
 * global error handling, auto-retries, and prevent UI double-submission.
 */
(function (window) {
    'use strict';

    // Track active requests to handle deduplication and prevent race conditions
    const activeRequests = new Map();

    /**
     * Extracts the __RequestVerificationToken from the document context
     */
    function getAntiforgeryToken() {
        // Look for common token selectors
        const tokenInput = document.querySelector('input[name="__RequestVerificationToken"]');
        if (tokenInput) return tokenInput.value;

        // Try getting it from header meta if available
        const metaToken = document.querySelector('meta[name="csrf-token"]');
        if (metaToken) return metaToken.content;

        return '';
    }

    /**
     * Executes a global error action based on response status code
     */
    function handleGlobalErrors(status, statusText) {
        switch (status) {
            case 401:
                console.warn('Unauthorized request. Redirecting/reloading...', statusText);
                window.location.reload();
                break;
            case 403:
                console.error('Forbidden access. Permissions missing.', statusText);
                if (typeof window.showNotificationToast === 'function') {
                    window.showNotificationToast({ title: 'Access Denied', content: 'You do not have permission to perform this action.' });
                }
                break;
            case 500:
                console.error('Internal Server Error.', statusText);
                if (typeof window.showNotificationToast === 'function') {
                    window.showNotificationToast({ title: 'Server Error', content: 'Something went wrong on our servers. Please try again.' });
                }
                break;
            default:
                break;
        }
    }

    /**
     * Unified fetch call with auto-retries, deduplication, and CSRF support
     */
    async function request(url, options = {}) {
        const method = (options.method || 'GET').toUpperCase();
        const deduplicate = options.deduplicate !== false;
        const maxRetries = options.retries !== undefined ? options.retries : (method === 'GET' ? 3 : 0);
        const retryDelay = options.retryDelay || 1000;

        // 1. Handle Request Deduplication via AbortController
        if (deduplicate) {
            const requestKey = `${method}:${url}`;
            if (activeRequests.has(requestKey)) {
                const oldController = activeRequests.get(requestKey);
                oldController.abort(); // Cancel the previous pending request
                activeRequests.delete(requestKey);
            }

            const controller = new AbortController();
            options.signal = controller.signal;
            activeRequests.set(requestKey, controller);
        }

        // 2. Prepare headers (CSRF injector)
        options.headers = options.headers || {};
        
        // Auto-inject Request Verification Token for modifying states
        if (['POST', 'PUT', 'DELETE'].includes(method)) {
            const token = getAntiforgeryToken();
            if (token) {
                options.headers['RequestVerificationToken'] = token;
                options.headers['X-CSRF-TOKEN'] = token;
            }
        }

        // Set default content type if not specified and body is object
        if (options.body && typeof options.body === 'object' && !(options.body instanceof FormData)) {
            options.headers['Content-Type'] = 'application/json';
            options.body = JSON.stringify(options.body);
        }

        // 3. Request Execution with Retry Mechanism
        let attempt = 0;
        while (attempt <= maxRetries) {
            try {
                attempt++;
                const response = await fetch(url, options);

                // Clean up tracking on successful return
                if (deduplicate) {
                    activeRequests.delete(`${method}:${url}`);
                }

                if (!response.ok) {
                    handleGlobalErrors(response.status, response.statusText);
                    throw new Error(`HTTP Error ${response.status}: ${response.statusText}`);
                }

                // Parse content based on type header
                const contentType = response.headers.get('content-type');
                if (contentType && contentType.includes('application/json')) {
                    return await response.json();
                }
                return await response.text();

            } catch (error) {
                // Do not retry or log if aborted intentionally
                if (error.name === 'AbortError') {
                    throw error;
                }

                const isNetworkError = error.message.includes('Failed to fetch') || error.message.includes('NetworkError');
                if (attempt <= maxRetries && isNetworkError) {
                    console.warn(`API network issue. Retrying request ${attempt}/${maxRetries} to ${url} in ${retryDelay}ms...`);
                    await new Promise(resolve => setTimeout(resolve, retryDelay));
                } else {
                    if (deduplicate) {
                        activeRequests.delete(`${method}:${url}`);
                    }
                    throw error;
                }
            }
        }
    }

    // Export API wrapper functions globally and freeze to prevent tampering
    window.ApiClient = Object.freeze({
        get: (url, options = {}) => request(url, { ...options, method: 'GET' }),
        post: (url, body, options = {}) => request(url, { ...options, method: 'POST', body }),
        put: (url, body, options = {}) => request(url, { ...options, method: 'PUT', body }),
        delete: (url, options = {}) => request(url, { ...options, method: 'DELETE' })
    });

})(window);
