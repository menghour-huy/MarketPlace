// ============================================================
//  AppJs — Global CRUD helper + Toast notification system
//  Usage:
//    AppJs.crud.post(url, formDataOrObject, options)
//    AppJs.crud.get(url, params, options)
//    AppJs.crud.put(url, formDataOrObject, options)
//    AppJs.crud.delete(url, options)
//    AppJs.toast.success(message)  |  AppJs.toast.error(message)
//    AppJs.toast.info(message)     |  AppJs.toast.warning(message)
// ============================================================

var AppJs = (function ($) {
    'use strict';

    // ── Toast ────────────────────────────────────────────────
    var _toastContainerId = 'AppJs-toast-container';

    function _ensureContainer() {
        if ($('#' + _toastContainerId).length === 0) {
            $('body').append(
                '<div id="' + _toastContainerId + '" ' +
                'style="position:fixed;top:24px;right:24px;z-index:9999;display:flex;flex-direction:column;gap:10px;min-width:300px;max-width:380px;">' +
                '</div>'
            );
        }
        return $('#' + _toastContainerId);
    }

    var _toastConfig = {
        success: { bg: '#22c55e', icon: 'fa-check-circle',    label: 'Success' },
        error:   { bg: '#ef4444', icon: 'fa-times-circle',    label: 'Error'   },
        warning: { bg: '#f97316', icon: 'fa-exclamation-triangle', label: 'Warning' },
        info:    { bg: '#4dabf7', icon: 'fa-info-circle',     label: 'Info'    }
    };

    function _showToast(type, message, duration) {
        duration = duration || 4000;
        var cfg = _toastConfig[type] || _toastConfig.info;
        var id  = 'toast-' + Date.now();

        var html =
            '<div id="' + id + '" style="' +
            'background:#fff;border-radius:12px;box-shadow:0 4px 24px rgba(0,0,0,0.13);' +
            'overflow:hidden;display:flex;align-items:stretch;animation:slideIn .3s ease;">' +
              '<div style="width:6px;background:' + cfg.bg + ';flex-shrink:0;"></div>' +
              '<div style="display:flex;align-items:center;gap:12px;padding:14px 16px;flex:1;">' +
                '<i class="fa ' + cfg.icon + '" style="color:' + cfg.bg + ';font-size:20px;flex-shrink:0;"></i>' +
                '<div style="flex:1;">' +
                  '<div style="font-weight:700;font-size:13px;color:#1a1a2e;">' + cfg.label + '</div>' +
                  '<div style="font-size:13px;color:#4b5563;margin-top:2px;">' + _escapeHtml(message) + '</div>' +
                '</div>' +
                '<button onclick="AppJs.toast.dismiss(\'' + id + '\')" ' +
                'style="background:none;border:none;font-size:16px;color:#9ca3af;cursor:pointer;padding:0 4px;line-height:1;">&times;</button>' +
              '</div>' +
            '</div>';

        var $container = _ensureContainer();
        $container.append(html);

        // auto-dismiss
        if (duration > 0) {
            setTimeout(function () { AppJs.toast.dismiss(id); }, duration);
        }
    }

    function _escapeHtml(str) {
        return String(str)
            .replace(/&/g, '&amp;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;')
            .replace(/"/g, '&quot;');
    }

    // inject keyframe once
    if (!$('#AppJs-toast-style').length) {
        $('<style id="AppJs-toast-style">' +
            '@keyframes slideIn{from{opacity:0;transform:translateX(40px)}to{opacity:1;transform:translateX(0)}}' +
            '@keyframes slideOut{from{opacity:1;transform:translateX(0)}to{opacity:0;transform:translateX(40px)}}' +
          '</style>').appendTo('head');
    }

    var toast = {
        success: function (msg, duration) { _showToast('success', msg, duration); },
        error:   function (msg, duration) { _showToast('error',   msg, duration); },
        warning: function (msg, duration) { _showToast('warning', msg, duration); },
        info:    function (msg, duration) { _showToast('info',    msg, duration); },
        dismiss: function (id) {
            var $el = $('#' + id);
            $el.css({ animation: 'slideOut .3s ease forwards' });
            setTimeout(function () { $el.remove(); }, 300);
        }
    };

    // ── CRUD core ────────────────────────────────────────────
    /**
     * options = {
     *   headers       : {},          // extra headers
     *   contentType   : '...',       // default: auto-detect (form / json)
     *   onSuccess     : fn(data),    // called on success:true
     *   onError       : fn(data),    // called on success:false or HTTP error
     *   onComplete    : fn(),        // always called
     *   showToast     : true,        // show built-in toast (default true)
     *   redirectDelay : 800,         // ms before redirect (default 800)
     *   $btn          : $element,    // button to disable/restore during request
     *   btnLoadingText: '...'        // text while loading
     * }
     */
    function _request(method, url, data, options) {
        options = $.extend({
            showToast     : true,
            redirectDelay : 800,
            headers       : {}
        }, options || {});

        var $btn         = options.$btn;
        var originalHtml = $btn ? $btn.html() : null;
        var loadingText  = options.btnLoadingText || '<i class="fa fa-spinner fa-spin me-2"></i> Please wait…';

        // disable button
        if ($btn) {
            $btn.prop('disabled', true).html(loadingText);
        }

        // build ajax settings
        var ajaxSettings = {
            url    : url,
            type   : method,
            headers: $.extend({ 'X-Requested-With': 'XMLHttpRequest' }, options.headers),
            success: function (data) {
                if (data && data.success) {
                    if (options.showToast) toast.success(data.message || 'Operation completed.');
                    if (typeof options.onSuccess === 'function') options.onSuccess(data);
                    if (data.redirectUrl) {
                        setTimeout(function () { window.location.href = data.redirectUrl; }, options.redirectDelay);
                    }
                } else {
                    if (options.showToast) toast.error(data && data.message ? data.message : 'Something went wrong.');
                    if (typeof options.onError === 'function') options.onError(data);
                }
            },
            error: function (xhr) {
                var msg = 'An unexpected error occurred. Please try again.';
                try {
                    var parsed = JSON.parse(xhr.responseText);
                    if (parsed && parsed.message) msg = parsed.message;
                } catch (e) { /* ignore */ }
                if (options.showToast) toast.error(msg);
                if (typeof options.onError === 'function') options.onError({ success: false, message: msg, xhr: xhr });
            },
            complete: function () {
                if ($btn) {
                    $btn.prop('disabled', false).html(originalHtml);
                }
                if (typeof options.onComplete === 'function') options.onComplete();
            }
        };

        // handle data / content-type
        if (data instanceof FormData) {
            ajaxSettings.data        = data;
            ajaxSettings.processData = false;
            ajaxSettings.contentType = false;
        } else if (typeof data === 'string') {
            // serialized form string  e.g. $form.serialize()
            ajaxSettings.data        = data;
            ajaxSettings.contentType = options.contentType || 'application/x-www-form-urlencoded; charset=UTF-8';
        } else if (data && typeof data === 'object') {
            ajaxSettings.data        = JSON.stringify(data);
            ajaxSettings.contentType = options.contentType || 'application/json';
        }

        return $.ajax(ajaxSettings);
    }

    var crud = {
        /**
         * POST a form or plain object to a controller action.
         * @param {string} url
         * @param {string|object|FormData} data  — $form.serialize(), plain object, or FormData
         * @param {object} options
         */
        post: function (url, data, options) {
            return _request('POST', url, data, options);
        },

        /** GET request — data is appended as query string */
        get: function (url, params, options) {
            options = options || {};
            if (params) url += (url.indexOf('?') === -1 ? '?' : '&') + $.param(params);
            return _request('GET', url, null, options);
        },

        /** PUT request */
        put: function (url, data, options) {
            return _request('PUT', url, data, options);
        },

        /** DELETE request */
        delete: function (url, options) {
            return _request('DELETE', url, null, options);
        },

        /**
         * Convenience: wire up a form to submit via AJAX automatically.
         * @param {string|jQuery} formSelector
         * @param {object} options  — same as _request options
         *
         * Example:
         *   AppJs.crud.bindForm('#frmRegister', { $btn: $('#btnCreate') });
         */
        bindForm: function (formSelector, options) {
            $(document).on('submit', formSelector, function (e) {
                e.preventDefault();
                var $form = $(this);
                // run jQuery validation if present
                if ($form.data('validator') && !$form.valid()) return;
                var url  = $form.attr('action') || window.location.href;
                var data = $form.serialize();
                AppJs.crud.post(url, data, options);
            });
        }
    };

    return { toast: toast, crud: crud };

})(jQuery);

// ============================================================
//  AppJs.theme — Light / Dark mode toggle
//  Stored in localStorage under key 'app-theme' ('light'|'dark')
//  Relies on Bootstrap 5.3 data-bs-theme attribute on <html>
// ============================================================
AppJs.theme = (function () {
    'use strict';

    var STORAGE_KEY = 'app-theme';
    var ICON_DARK   = '\uD83C\uDF19';   // 🌙  shown when current theme is light (click → go dark)
    var ICON_LIGHT  = '\u2600\uFE0F';   // ☀️  shown when current theme is dark  (click → go light)

    function _current() {
        return document.documentElement.getAttribute('data-bs-theme') || 'light';
    }

    function _syncIcon(t) {
        var $icon = $('#themeIcon');
        if ($icon.length) {
            $icon.text(t === 'dark' ? ICON_LIGHT : ICON_DARK);
        }
    }

    function apply(t) {
        t = (t === 'dark') ? 'dark' : 'light';
        document.documentElement.setAttribute('data-bs-theme', t);
        localStorage.setItem(STORAGE_KEY, t);
        _syncIcon(t);
    }

    function toggle() {
        apply(_current() === 'dark' ? 'light' : 'dark');
    }

    function init() {
        // The <head> inline script already set the attribute to avoid flash.
        // Here we just sync the icon after jQuery/DOM is ready.
        _syncIcon(_current());
    }

    // Auto-init when DOM is ready
    $(function () { init(); });

    return { apply: apply, toggle: toggle, current: _current };
}());
