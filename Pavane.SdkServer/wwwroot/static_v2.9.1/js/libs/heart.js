 // 心跳 =====
 function lcx_heartbeat(param){
    var _param = param
    var u = navigator.userAgent;
    var isAndroid = u.indexOf('Android') > -1 || u.indexOf('Adr') > -1; //android终端
    var isiOS = !!u.match(/\(i[^;]+;( U;)? CPU.+Mac OS X/); //ios终端
  
    // 心跳返回
    if(isiOS && typeof(window.webkit) != undefined){
      window.webkit.messageHandlers['callNativeAPI'].postMessage(['lcx_heartbeat', _param]);
    }
  
    if(isAndroid && typeof(jsInterface) != undefined){
      jsInterface.callNativeAPI('lcx_heartbeat', _param)
    }
  
    console.log("do lcx heartbeat:" + _param)
  }
  // 心跳 =====