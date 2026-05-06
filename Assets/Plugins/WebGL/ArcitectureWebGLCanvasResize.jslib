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

    state.syncCanvasSize = function () {
      var canvas = state.resolveCanvas();
      if (!canvas) {
        return 0;
      }

      var rect = canvas.getBoundingClientRect ? canvas.getBoundingClientRect() : null;
      var cssWidth = Math.round((rect && rect.width) || canvas.clientWidth || canvas.width || 0);
      var cssHeight = Math.round((rect && rect.height) || canvas.clientHeight || canvas.height || 0);
      if (cssWidth <= 0 || cssHeight <= 0) {
        return 0;
      }

      var ratio = Math.max(1, (typeof window !== "undefined" && window.devicePixelRatio) || 1);
      var targetWidth = Math.max(1, Math.round(cssWidth * ratio));
      var targetHeight = Math.max(1, Math.round(cssHeight * ratio));
      var changed = canvas.width !== targetWidth || canvas.height !== targetHeight;

      if (changed) {
        if (typeof Browser !== "undefined" && Browser.setCanvasSize) {
          Browser.setCanvasSize(targetWidth, targetHeight, true);
        } else {
          canvas.width = targetWidth;
          canvas.height = targetHeight;
        }
      }

      if (typeof GLctx !== "undefined" && GLctx) {
        GLctx.viewport(0, 0, targetWidth, targetHeight);
      }

      return changed ? 1 : 0;
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
  }
});
