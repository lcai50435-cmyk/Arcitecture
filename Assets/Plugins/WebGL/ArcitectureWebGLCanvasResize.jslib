mergeInto(LibraryManager.library, {
  ArcitectureInstallCanvasResizeBridge: function (receiverNamePtr) {
    var receiverName = UTF8ToString(receiverNamePtr);
    var state = Module.arcitectureCanvasResizeState || (Module.arcitectureCanvasResizeState = {});
    state.receiverName = receiverName;

    if (state.installed) {
      state.scheduleRefresh();
      return;
    }

    state.installed = true;
    state.pendingTimers = [];
    state.lastWindowedCssWidth = state.lastWindowedCssWidth || 0;
    state.lastWindowedCssHeight = state.lastWindowedCssHeight || 0;

    state.minimumStableCssWidth = 480;
    state.minimumStableCssHeight = 270;
    state.defaultWindowedCssWidth = 960;
    state.defaultWindowedCssHeight = 600;

    state.notifyUnity = function () {
      if (!state.receiverName || typeof SendMessage !== "function") {
        return;
      }

      try {
        SendMessage(state.receiverName, "HandleWebGLCanvasResize", "");
      } catch (error) {
        if (typeof console !== "undefined" && console.debug) {
          console.debug("Arcitecture WebGL resize notify failed", error);
        }
      }
    };

    state.resolveCanvas = function () {
      return Module.canvas || (typeof document !== "undefined" && document.querySelector("canvas"));
    };

    state.isFullscreen = function () {
      if (typeof document === "undefined") {
        return false;
      }

      return !!(
        document.fullscreenElement ||
        document.webkitFullscreenElement ||
        document.mozFullScreenElement ||
        document.msFullscreenElement
      );
    };

    state.getViewportSize = function () {
      if (typeof window === "undefined") {
        return { width: 0, height: 0 };
      }

      var element = typeof document !== "undefined" ? document.documentElement : null;
      return {
        width: Math.round(window.innerWidth || (element && element.clientWidth) || 0),
        height: Math.round(window.innerHeight || (element && element.clientHeight) || 0)
      };
    };

    state.readCanvasCssSize = function (canvas) {
      var rect = canvas.getBoundingClientRect ? canvas.getBoundingClientRect() : null;
      var cssWidth = Math.round((rect && rect.width) || canvas.clientWidth || 0);
      var cssHeight = Math.round((rect && rect.height) || canvas.clientHeight || 0);
      if (cssWidth <= 0 || cssHeight <= 0) {
        return null;
      }

      return { width: cssWidth, height: cssHeight };
    };

    state.readContainerCssSize = function (canvas) {
      if (!canvas) {
        return null;
      }

      var candidates = [];
      if (canvas.parentElement) {
        candidates.push(canvas.parentElement);
      }

      if (typeof document !== "undefined" && document.getElementById) {
        var unityContainer = document.getElementById("unity-container");
        if (unityContainer && unityContainer !== canvas.parentElement) {
          candidates.push(unityContainer);
        }
      }

      var best = null;
      for (var i = 0; i < candidates.length; i++) {
        var element = candidates[i];
        if (!element) {
          continue;
        }

        var rect = element.getBoundingClientRect ? element.getBoundingClientRect() : null;
        var width = Math.round((rect && rect.width) || element.clientWidth || 0);
        var height = Math.round((rect && rect.height) || element.clientHeight || 0);
        if (width < state.minimumStableCssWidth || height < state.minimumStableCssHeight) {
          continue;
        }

        if (!best || width * height > best.width * best.height) {
          best = { width: width, height: height };
        }
      }

      return best;
    };

    state.readBackingStoreAsCssSize = function (canvas) {
      var ratio = Math.max(1, (typeof window !== "undefined" && window.devicePixelRatio) || 1);
      var width = Math.round((canvas.width || 0) / ratio);
      var height = Math.round((canvas.height || 0) / ratio);
      if (width <= 0 || height <= 0) {
        return null;
      }

      return { width: width, height: height };
    };

    state.resolveExpectedAspect = function (canvas) {
      if (state.lastWindowedCssWidth > 0 && state.lastWindowedCssHeight > 0) {
        return state.lastWindowedCssWidth / Math.max(1, state.lastWindowedCssHeight);
      }

      var backingSize = state.readBackingStoreAsCssSize(canvas);
      if (backingSize) {
        return backingSize.width / Math.max(1, backingSize.height);
      }

      return state.defaultWindowedCssWidth / Math.max(1, state.defaultWindowedCssHeight);
    };

    state.resolveExpectedFromContainer = function (canvas) {
      var containerSize = state.readContainerCssSize(canvas);
      if (!containerSize) {
        return null;
      }

      var aspect = state.resolveExpectedAspect(canvas);
      var targetWidth = containerSize.width;
      var targetHeight = Math.round(targetWidth / Math.max(0.01, aspect));
      if (targetHeight > containerSize.height) {
        targetHeight = containerSize.height;
        targetWidth = Math.round(targetHeight * aspect);
      }

      if (targetWidth < state.minimumStableCssWidth || targetHeight < state.minimumStableCssHeight) {
        return null;
      }

      return { width: targetWidth, height: targetHeight };
    };

    state.rememberWindowedSize = function (size) {
      if (!size || state.isFullscreen()) {
        return;
      }

      if (size.width >= state.minimumStableCssWidth && size.height >= state.minimumStableCssHeight) {
        state.lastWindowedCssWidth = size.width;
        state.lastWindowedCssHeight = size.height;
      }
    };

    state.resolveExpectedWindowedSize = function (canvas) {
      if (state.lastWindowedCssWidth > 0 && state.lastWindowedCssHeight > 0) {
        return {
          width: state.lastWindowedCssWidth,
          height: state.lastWindowedCssHeight
        };
      }

      var containerSize = state.resolveExpectedFromContainer(canvas);
      if (containerSize) {
        return containerSize;
      }

      var backingSize = state.readBackingStoreAsCssSize(canvas);
      if (backingSize &&
          backingSize.width >= state.minimumStableCssWidth &&
          backingSize.height >= state.minimumStableCssHeight) {
        return backingSize;
      }

      return {
        width: state.defaultWindowedCssWidth,
        height: state.defaultWindowedCssHeight
      };
    };

    state.isSuspiciousWindowedSize = function (canvas, size) {
      if (!canvas || !size || state.isFullscreen()) {
        return false;
      }

      var expected = state.resolveExpectedWindowedSize(canvas);
      if (!expected || expected.width <= 0 || expected.height <= 0) {
        return false;
      }

      var viewport = state.getViewportSize();
      var viewportCanFitExpected =
        viewport.width >= Math.round(expected.width * 0.85) &&
        viewport.height >= Math.round(expected.height * 0.85);
      if (!viewportCanFitExpected) {
        return false;
      }

      return size.width < Math.round(expected.width * 0.75) &&
        size.height < Math.round(expected.height * 0.75);
    };

    state.restoreWindowedCssSize = function (canvas, size) {
      if (!canvas || !canvas.style || !size) {
        return;
      }

      canvas.style.width = size.width + "px";
      canvas.style.height = size.height + "px";
    };

    state.resolveTargetCssSize = function (canvas) {
      var size = state.readCanvasCssSize(canvas);
      if (!size) {
        return null;
      }

      if (state.isSuspiciousWindowedSize(canvas, size)) {
        var expected = state.resolveExpectedWindowedSize(canvas);
        state.restoreWindowedCssSize(canvas, expected);
        state.rememberWindowedSize(expected);
        return expected;
      }

      state.rememberWindowedSize(size);
      return size;
    };

    state.syncCanvasSize = function () {
      var canvas = state.resolveCanvas();
      if (!canvas) {
        return 0;
      }

      var cssSize = state.resolveTargetCssSize(canvas);
      if (!cssSize || cssSize.width <= 0 || cssSize.height <= 0) {
        return 0;
      }

      var ratio = Math.max(1, (typeof window !== "undefined" && window.devicePixelRatio) || 1);
      var targetWidth = Math.max(1, Math.round(cssSize.width * ratio));
      var targetHeight = Math.max(1, Math.round(cssSize.height * ratio));
      var changed = canvas.width !== targetWidth || canvas.height !== targetHeight;

      if (changed) {
        if (typeof Browser !== "undefined" && Browser.setCanvasSize) {
          Browser.setCanvasSize(targetWidth, targetHeight, false);
        } else {
          canvas.width = targetWidth;
          canvas.height = targetHeight;
        }
      }

      state.restoreWindowedCssSize(canvas, cssSize);

      if (typeof GLctx !== "undefined" && GLctx) {
        GLctx.viewport(0, 0, canvas.width || targetWidth, canvas.height || targetHeight);
      }

      return changed ? 1 : 0;
    };

    state.restoreCanvasViewport = function () {
      var canvas = state.resolveCanvas();
      if (!canvas || typeof GLctx === "undefined" || !GLctx) {
        return 0;
      }

      var width = canvas.width || 0;
      var height = canvas.height || 0;
      if (width <= 0 || height <= 0) {
        return 0;
      }

      GLctx.viewport(0, 0, width, height);
      return 1;
    };

    state.scheduleRefresh = function () {
      var delays = [0, 16, 50, 100, 250, 500, 1000];
      for (var i = 0; i < state.pendingTimers.length; i++) {
        clearTimeout(state.pendingTimers[i]);
      }

      state.pendingTimers.length = 0;
      for (var delayIndex = 0; delayIndex < delays.length; delayIndex++) {
        state.pendingTimers.push(setTimeout(function () {
          state.syncCanvasSize();
          state.notifyUnity();
        }, delays[delayIndex]));
      }

      if (typeof requestAnimationFrame === "function") {
        requestAnimationFrame(function () {
          state.syncCanvasSize();
          state.notifyUnity();
        });
      }
    };

    if (typeof window !== "undefined" && window.addEventListener) {
      var events = ["resize", "orientationchange", "focus", "pageshow"];
      for (var eventIndex = 0; eventIndex < events.length; eventIndex++) {
        window.addEventListener(events[eventIndex], state.scheduleRefresh, true);
      }
    }

    if (typeof document !== "undefined" && document.addEventListener) {
      document.addEventListener("fullscreenchange", state.scheduleRefresh, true);
      document.addEventListener("webkitfullscreenchange", state.scheduleRefresh, true);
      document.addEventListener("mozfullscreenchange", state.scheduleRefresh, true);
      document.addEventListener("MSFullscreenChange", state.scheduleRefresh, true);
      document.addEventListener("keyup", function (event) {
        if (event && (event.key === "Escape" || event.keyCode === 27)) {
          state.scheduleRefresh();
        }
      }, true);
    }

    state.scheduleRefresh();
  },

  ArcitectureSyncCanvasSize: function () {
    var state = Module.arcitectureCanvasResizeState;
    if (!state || !state.syncCanvasSize) {
      return 0;
    }

    return state.syncCanvasSize();
  },

  ArcitectureRestoreCanvasViewport: function () {
    var state = Module.arcitectureCanvasResizeState;
    if (state && state.restoreCanvasViewport) {
      return state.restoreCanvasViewport();
    }

    var canvas = Module.canvas || (typeof document !== "undefined" && document.querySelector("canvas"));
    if (!canvas || typeof GLctx === "undefined" || !GLctx || !canvas.width || !canvas.height) {
      return 0;
    }

    GLctx.viewport(0, 0, canvas.width, canvas.height);
    return 1;
  }
});
