// 防止vue起不来
window.onerror = function(msg, url, lineNo, columnNo, erro){
    var _errorJson = {
      msg:msg,
      url:url,
      lineNo:lineNo,
      columnNo:columnNo
    }
    var errorToSdk = JSON.stringify(_errorJson)
  
    // 上报客户端
    console.log("report to sdk");
    var u = navigator.userAgent;
    var isAndroid = u.indexOf('Android') > -1 || u.indexOf('Adr') > -1; //android终端
    var isiOS = !!u.match(/\(i[^;]+;( U;)? CPU.+Mac OS X/); //ios终端
  
    if(isiOS && typeof(window.webkit) != undefined){
      window.webkit.messageHandlers['callNativeAPI'].postMessage(['nativeLog', errorToSdk]);
      window.webkit.messageHandlers['callNativeAPI'].postMessage(['lcx_web_log', errorToSdk]);
    }
  
    if(isAndroid && typeof(jsInterface) != undefined){
      jsInterface.callNativeAPI('nativeLog', errorToSdk)
      jsInterface.callNativeAPI('lcx_web_log', errorToSdk)
    }
  }
  // jsbridge =====
  function callH5API(functionName,param) {
    console.log(functionName)
    console.log(param)
    var _param = JSON.stringify(param)
    eval(functionName+'('+_param+')')
  }

