module.exports = {
  '/auth': {
    target: 'http://localhost:5073',
    secure: false,
    changeOrigin: true
  },
  '/innovations': {
    target: 'http://localhost:5073',
    secure: false,
    changeOrigin: true,
    bypass: function (req) {
      // Browser HTML navigations (e.g. address bar, page reload) must be served by the
      // Angular SPA so the router can handle the URL. Only XHR/fetch API calls should be
      // forwarded to the backend.
      if (req.headers.accept && req.headers.accept.includes('text/html')) {
        return '/index.html';
      }
    }
  },
  '/industries': {
    target: 'http://localhost:5073',
    secure: false,
    changeOrigin: true
  },
  '/bids': {
    target: 'http://localhost:5073',
    secure: false,
    changeOrigin: true
  }
};
