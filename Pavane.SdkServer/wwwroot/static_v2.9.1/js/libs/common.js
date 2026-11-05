var app = new Vue({
    el: '#app',
    data: {
      label : {},
      agreementContentList: [],
      agreementContent: '',
      agreementContentKr0: '',
      agreementContentKr1: '',
      param: '',
      pageCurrentStatus: {
        fileStep: 0
      },
      showFirstPage: false,
      showContentPage: false,
      showContentPageKr: false,
      showBackBtn: false,
      showBackBtnKr: false,
      showLoading: true,
      downArrowShow: false,
      isReadAll: false,
      isReadAllKr: [false, false],
      checkBoxStatus: false,
      checkBoxStatus0: false,
      checkBoxStatus1: false,
      iconShake: '',
      iconShake0: '',
      iconShake1: '',
      tipClass: '',
      tipErrorText: '',
      agreeAbleClass: '',
      agreeClickBtn: true,
      requeststamp: "",
      showSlideImg: false,
      isFirstScroll: true,
      isFirstScroll0: true,
      isFirstScroll1: true
    },
    created() {
      this.errorHander()
      this.logAli("agreement_created_success",{
        message: "success"
      })
    },
    watch: {
      downArrowShow: function(val) {
        var that = this
        if(val) {
          setTimeout(function(){
            that.downArrowShow = false
          },5000)
        }
      },
      checkBoxStatus: function(val) {
        this.logAli("agreement_checkbox",{
          message: "success",
          checkboxStatus: val.toString()
        })          
      },
      checkBoxStatus0: function(val) {
        this.logAli("agreement_checkbox",{
          message: "success",
          checkbox0Status: val.toString()
        })          
      },
      checkBoxStatus1: function(val) {
        this.logAli("agreement_checkbox",{
          message: "success",
          checkbox1Status: val.toString()
        })          
      },            
    },
    mounted() {
      this.logAli("agreement_mounted_success",{
        message: "success"
      })      
      this.init()
      this.showLoading = false
      this.getLang()
      var that = this
      var element = document.getElementById('includeContent');
      element.addEventListener('scroll', function(event) {
        if (!that.checkBoxStatus) {
          that.showShake()            
        }           
        if (that.isFirstScroll) {
          that.logAli("agreement_slide",{
            message: "success"
          })          
          that.isFirstScroll = false
        }
      });  
      var elementKr0 = document.getElementById('includeContentKr0');
      elementKr0.addEventListener('scroll', function(event) {
        if (!that.checkBoxStatus0) {
          that.showShake0()
        }          
        if (that.isFirstScroll0) {
          that.logAli("agreement_slide",{
            message: "kr0_slide_success"
          })          
          that.isFirstScroll0 = false
        }
      });      
      var elementKr1 = document.getElementById('includeContentKr1');
      elementKr1.addEventListener('scroll', function(event) {
        if (!that.checkBoxStatus1) {
          that.showShake1()
        }           
        if (that.isFirstScroll1) {
          that.logAli("agreement_slide",{
            message: "kr1_slide_success"
          })          
          that.isFirstScroll1 = false
        }
      });  


    },
    methods: {
        init() {
          // read cdn host, decide publish, env
          // cn exp:  https://lcmx-pp-cdn.mobage.cn/gameid/xxxxxx
          // ww exp: https://lcmx-pp-cdn.mobage.world/gameid-test/xxxxx
          console.log("now init")
          var cdnHostName = window.location.hostname,
              pathName = window.location.pathname,
              gameId = pathName.split("/")[1].split("-")[0],
              env = pathName.split("/")[1].split("-")[1] || "prod",
              publish, filesList;
          switch (cdnHostName) {
            case "lcmx-pp-cdn.mobage.world":
              publish = "ww"
              break;
            case "lcmx-pp-cdn.mobage.cn":
              publish = "cn"
              break;
              // for debug, can be deleted after test ---- start
            default:
              publish = this.getUrlParam("publish")
              break;
              // for debug, can be deleted after test ---- end
          }
          console.log("publish: " + publish)
          // according to publish, make default files list
          switch (publish) {
            case "cn":
              filesList = ["agreement", "policy"];
              break;
            case "ww":
              filesList = ["agreement"]
              break;
            default:
              filesList = ["agreement"]
              break;
          }

          var folder = this.getUrlParam("folder") || "default",
              region = this.getUrlParam("region"),
              fileName = decodeURIComponent(this.getUrlParam("filename")),
              filetype = this.getUrlParam("filetype"),
              pageType;
              console.log('filetype: ' + filetype)
          switch (folder&&region) {
            case undefined :
              pageType = "firstVisit";
              break;
            // case 'de':
            //   pageType = "visited";
            //   break;
            default:
              // debugger
              pageType = "visited"
              break;
          }
          this.pageCurrentStatus.filetype = filetype
          this.pageCurrentStatus.publish = publish
          this.pageCurrentStatus.env = env
          if(fileName) {
            this.pageCurrentStatus.filesList = fileName.split(',')
          } else {
            this.pageCurrentStatus.filesList = filesList
          }
          this.pageCurrentStatus.gameId = gameId
          this.pageCurrentStatus.folder = folder
          this.pageCurrentStatus.region = region
          this.pageCurrentStatus.fileName = fileName
          console.log('666', this.pageCurrentStatus.fileName)
          this.pageCurrentStatus.pageType = pageType
          this.pageCurrentStatus.cdnHostName = cdnHostName
          this.requeststamp = this.getUrlParam("requeststamp") || ""

          // base setting
          switch (pageType) {
            case "firstVisit":
              this.runFirstVisitFlow()
              break;
            case "visited":
              this.runVisitedFlow()
              break;
          }
          var that = this
          this.$nextTick(function(){
            that.logAli("agreement_init_success",{
              message: "success"
            })
            if (region !== "KR") {
              that.showSlideImg = true
            }
          })
        },
        runFirstVisitFlow() {
          console.log("now runFirstVisitFlow")
          console.log('fileName',this.pageCurrentStatus.fileName)
          switch (this.pageCurrentStatus.publish) {
            case "cn":
              this.showContentPage = false
              this.showContentPageKr = false
              this.showFirstPage = true
              break;
            case "ww":
              if (this.pageCurrentStatus.region == "KR" && this.pageCurrentStatus.filesList.length == 2) {
                this.showContentPageKr = true
                this.showContentPage = false
                this.showFirstPage = false                
                this.showDetailKr(this.pageCurrentStatus.filesList[0],0)
                this.showDetailKr(this.pageCurrentStatus.filesList[1],1)           
              }else{
                this.showContentPage = true
                this.showDetail(this.pageCurrentStatus.fileName)
                this.showContentPageKr = false
                this.showFirstPage = false                 
              }
              break;
            default:
              // debugger
              this.showContentPage = false
              this.showFirstPage = true
              this.showContentPageKr = false
              break;
          }
        },
        runVisitedFlow() {
          console.log("now runVisitedFlow")
          if (this.pageCurrentStatus.publish ==="ww" && this.pageCurrentStatus.region === "KR" && this.pageCurrentStatus.filesList.length == 2) {
            this.showDetailKr(this.pageCurrentStatus.filesList[0],0)
            this.showDetailKr(this.pageCurrentStatus.filesList[1],1)
          }else{
            this.showDetail(this.pageCurrentStatus.fileName)
          }
        },
        getUrlParam(paramName) {
          // ******function URLSearchParams 不兼容木木模拟器*****
          // var searchURL = location.search;
          // console.log("searchURL:"+searchURL)
          // var params = new URLSearchParams(searchURL);   
          // console.log("params:"+params)
          // var valueObj = Object.fromEntries(params);
          // console.log("valueObj:")
          // console.log(valueObj)
          // var param = valueObj[paramName]
          // console.log("param:"+param)
          // this.param = param;
          // return param;
          // ******function URLSearchParams 不兼容木木模拟器*****
          var reg = new RegExp("(^|&)" + paramName + "=([^&]*)(&|$)", "i");
          var result = window.location.search.substr(1).match(reg);
          if ( result != null ){
            var param = decodeURI(result[2])
            this.param = param;
            return param;
          }else{
             return undefined;
          }
        },
        showDetail(fileName, cb) {
          console.log("now showDetail")
          console.log("now: fileName:")
          console.log(fileName)
          var contentUrl,
              that = this;
          // if (this.pageCurrentStatus.cdnHostName == "localhost") {
            // contentUrl = this.pageCurrentStatus.folder + "/" + fileName + ".html"
          // }else{
            // contentUrl = "http://localhost/15000000-test/" + this.pageCurrentStatus.folder + "/" + this.pageCurrentStatus.region + "/" + fileName + ".html?redirect=1";
            // contentUrl = this.pageCurrentStatus.folder + "/" + fileName + ".html"
          // }
          // contentUrl = this.pageCurrentStatus.folder + "/" + this.pageCurrentStatus.region + "/" + fileName + ".html?redirect=1"
          this.showLoading = true;
          this.showContentPage = true;
          console.log("now: showContentPage:")
          console.log(this.showContentPage)
          console.log("now: contentUrl:")
          console.log(contentUrl)

          this.showFirstPage = false
          console.log("now: showFirstPage:")
          console.log(this.showFirstPage)   
          // debugger       
          if (this.pageCurrentStatus.filetype == undefined) {
            this.showBackBtn = false;
          } else{
            this.showBackBtn = true;
          }
          if(fileName) {
            var fileNames = fileName.split(',')
            if(fileNames.length > 1) {
              this.agreementContentList = Array(fileNames.length).fill('')
              for (var index = 0; index < fileNames.length; index++) {
                var name = fileNames[index];
                 this.getHtmlInfo(name, cb,index)
                // if(isRequestError) return;
              }
            }else if(fileNames.length == 1){
              this.agreementContentList = Array(1).fill('')
              this.getHtmlInfo(fileName, cb,0)
            }
          }
        },
        getHtmlInfo(fileName, cb,index) {
          var that = this;
          // var requestStatus;
          var contentUrl;

          contentUrl = this.pageCurrentStatus.folder + "/" + this.pageCurrentStatus.region + "/" + fileName + ".html?redirect=1"
          // try {
            // var res = await axios.get(contentUrl)
            axios.get(contentUrl).then(function(res){
            console.log("now: get contentUrl")
            if (res.status == 200) {
                // that.agreementContent = that.agreementContent+res.data
                // console.log('that.agreementContentList[index]=res.data',that.agreementContentList,index,res.data);
                  that.agreementContentList[index]=res.data
                  that.agreementContent = that.agreementContentList.join('')
                  that.showLoading = false
                that.$nextTick(function(){
                  if (typeof(cb) === "function") {
                    cb()
                  }
                })
              }
            }).catch(function(res){
          // } catch (error) {
            if (res.response.status == 404) {
              that.logAli("agreement_error",{
                message: "404"
              })
              // if (that.pageCurrentStatus.cdnHostName == "localhost") {
                // contentUrl = "default" + "/agreement.html"
              // }else{
                // contentUrl = "https://" + that.pageCurrentStatus.cdnHostName + "/" + that.pageCurrentStatus.gameId + "/default/agreement.html";
                // contentUrl = "default" + "/agreement.html"
              // }
              // const souce = this.getUrlParam("souce")
              contentUrl = "ALL/ALL/"+ fileName + ".html?redirect=1"
              console.log("after 404 url:" + contentUrl)
              // try {
                axios.get(contentUrl).then(function(params){
                // var params = await axios.get(contentUrl)
                if (params.status == 200) {
                  // that.agreementContent = that.agreementContent+params.data
                  that.agreementContentList[index]=params.data
                  that.agreementContent = that.agreementContentList.join('')
                  that.showLoading = false
                  that.$nextTick(function(){
                    if (typeof(cb) === "function") {
                      cb()
                    }
                  })
                  // requestStatus =  true
                }
              })
              // } catch (error) {
                
              // }
            }
            that.showLoading = false
            })
          // }
          // return requestStatus
        },
        showDetailKr(fileName, position, cb) {
          console.log("now showDetail")
          console.log("now: fileName:")
          console.log(fileName)

          var contentUrl,
              that = this;
          // if (this.pageCurrentStatus.cdnHostName == "localhost") {
            // contentUrl = this.pageCurrentStatus.folder + "/" + fileName + ".html"
          // }else{
            // contentUrl = "https://" + this.pageCurrentStatus.cdnHostName + "/" + this.pageCurrentStatus.gameId + "/" + this.pageCurrentStatus.folder + "/" + fileName + ".html";
            // contentUrl = this.pageCurrentStatus.folder + "/" + fileName + ".html"
          // }
          contentUrl = this.pageCurrentStatus.folder + "/" + this.pageCurrentStatus.region + "/" + fileName + ".html?redirect=1"

          this.showLoading = true;
          this.showContentPageKr = true;
          console.log("now: showKrContentPage:")
          console.log(this.showContentPageKr)
          console.log("now: contentUrl:")
          console.log(contentUrl)

          this.showFirstPage = false
          this.showContentPage = false
          console.log("now: showFirstPage:")
          console.log(this.showFirstPage)          
          if (this.pageCurrentStatus.publish == "ww" && this.pageCurrentStatus.filetype == undefined) {
            this.showBackBtnKr = false;
          }else{
            this.showBackBtnKr = true;
          }
          axios.get(contentUrl).then(function(res){
          console.log("now: get contentUrl")
          if (res.status == 200) {
              that[`agreementContentKr${position}`] = res.data
              that.showLoading = false
              that.$nextTick(function(){
                if (typeof(cb) === "function") {
                  cb()
                }
              })
            }
          }).catch(function(res){
            if (res.response.status == 404) {
              that.logAli("agreement_error",{
                message: "404"
              })
              // if (that.pageCurrentStatus.cdnHostName == "localhost") {
                // contentUrl = "default" + "/agreement.html"
              // }else{
                // contentUrl = "https://" + that.pageCurrentStatus.cdnHostName + "/" + that.pageCurrentStatus.gameId + "/default/agreement.html";
                // contentUrl = "default" + "/agreement.html"
              // }
              // contentUrl = "default" + "/" + fileName + ".html?redirect=1"
              contentUrl = "ALL/ALL/"+ fileName + ".html?redirect=1"
              console.log("after 404 url:" + contentUrl)
              axios.get(contentUrl).then(function(params) {
                if (params.status == 200) {
                  that[`agreementContentKr${position}`] = params.data
                  that.showLoading = false
                  that.$nextTick(function(){
                    if (typeof(cb) === "function") {
                      cb()
                    }
                  })
                }
              })
            }
            that.showLoading = false
          })
        }, 
        goBack() {
          if (this.pageCurrentStatus.pageType == "firstVisit") {
            this.showContentPage = false;
            this.showFirstPage = true
          }
          if (this.pageCurrentStatus.pageType == "visited") {
            window.location.href = "app://agreement?action=2"
          }
        },
        goNext() {
          this.readContent()
          if (this.checkSubmitStatus()) {
              this.userAgree()
          }else{
            return
          }
        },
        goNextKr() {
          this.readContentKr(0)
          this.readContentKr(1)
          if (this.checkSubmitStatusKr()) {
            this.userAgree()
          }else{
            return
          }
        },  
        readContent() {
          var isReadAll = false;
          if (!this.isFirstScroll) {
            isReadAll = true
            this.showSlideImg = false
          }else{
            isReadAll = false
            this.showSlideImg = true
          }
          this.isReadAll = isReadAll
          this.setSubmitBtn()
        },
        readContentKr(contentIndex) {
          var $contentWrap = document.getElementById("includeContentKr"+contentIndex.toString()),
              $content = document.getElementById("content"+contentIndex.toString()),
              contentMaxOffsetTop = $content.clientHeight - $contentWrap.clientHeight + 30;
   
          this.isReadAllKr[contentIndex] = true
          // this.isReadAll = this.isReadAllKr[0] && this.isReadAllKr[1]
          this.isReadAll = true
          this.showSlideImg = false

          this.setSubmitBtnKr()
        },  
        setSubmitBtn(){
          if (this.checkBoxStatus && this.isReadAll) {
            this.agreeAbleClass = "able"
          }else{
            this.agreeAbleClass = ""
          }
        },
        setSubmitBtnKr(){
          if (this.checkBoxStatus0 && this.checkBoxStatus1 &&this.isReadAll) {
            this.agreeAbleClass = "able"
          }else{
            this.agreeAbleClass = ""
          }
        },  
        checkSubmitStatus() {
          console.log('isReadAll', this.isReadAll)
          if (this.isReadAll) {
            this.downArrowShow = false
            this.showSlideImg = false
            this.removeTip()
          }else{
            this.downArrowShow = true
            this.showSlideImg = true
            this.showTip("readError")
          }

          if (this.checkBoxStatus) {
            this.removeShake()
          }else{
            this.showShake()
            this.showTip("agreeError")
          }
          if (this.checkBoxStatus && this.isReadAll) {
            return true
          }else{
            return false
          }
        },
        checkSubmitStatusKr() {
          if (this.isReadAll) {
            this.downArrowShow = false
            this.showSlideImg = false
            this.removeTip()
          }else{
            this.downArrowShow = true
            this.showSlideImg = true
            this.showTip("readError")
          }

          if (this.checkBoxStatus0) {
            this.removeShake0()
          }else{
            this.showShake0()
            this.showTip("agreeError")
          } 
          if(this.checkBoxStatus1){
            this.removeShake1()
          }else{
            this.showShake1()
            this.showTip("agreeError")
          }
          if (this.checkBoxStatus1 &&this.checkBoxStatus0 && this.isReadAll) {
            return true
          }else{
            return false
          }
        },  
        showShake(){
          this.iconShake = "shake"
        },
        showShake0(){
          this.iconShake0 = "shake"
        },
        showShake1(){
          this.iconShake1 = "shake"
        },   
        removeShake() {
          this.iconShake = ""
        },
        removeShake0() {
          this.iconShake0 = ""
        },
        removeShake1() {
          this.iconShake1 = ""
        },
        showTip(labelString) {
          this.tipErrorText = this.$label("common."+labelString)
          this.tipClass = "a-fadeInOut"
        },
        removeTip() {
          this.tipClass = ""
        },
        logAli(action,info,callback) {
          if (callback && typeof (callback) === 'function') {
            callback()
          }   
          
          // var baseUrl = ''
          var baseUrl = "https://lcx-prod-ap.ap-northeast-1.log.aliyuncs.com/logstores/lcx-agreement-web/track";
          console.log(baseUrl)

          // agreement_init_success    页面加载完成
          // agreement_slide   首次滑动
          // agreement_checkbox  点击checkbox(韩国地区有两个checkbox）
          // agreement_agree    点击确认
          // agreement_error    错误
          // if (this.pageCurrentStatus.publish == "ww") {
          //   baseUrl = "https://lcx-prod-ap.ap-northeast-1.log.aliyuncs.com/logstores/lcx-agreement-web/track";
          // } else {
          //   return ;
          //   // baseUrl = "https://lcm-sdk-data.cn-hangzhou.log.aliyuncs.com/logstores/lcm_customer_service/track_ua.gif?APIVersion=0.6.0&district=cn";
          // }
          // console.log(baseUrl)
          var logs = [
            Object.assign(
              {},
              info,
              {
                action: action,
                storytype: this.getUrlParam("storeType") || undefined,
                productid: this.getUrlParam("productId") || undefined,
                countrycode: this.getUrlParam("countryCode") || undefined,
                sdkversion: this.getUrlParam("sdkVersion") || undefined,
                parkagename: this.getUrlParam("packageName") || undefined,
                requeststamp: this.getUrlParam("requeststamp") || undefined,
                folder: this.getUrlParam("folder") || undefined,
                sourceUrl: window.location.href
              }
            )
          ];                   
          var postData = {
            __source__: "sdkWeb",
            __topic__: "agreement3",
            __logs__: logs
          }
          // 将请求体转换为 JSON 字符串
          var requestBody = JSON.stringify(postData);
          // 获取请求体大小
          var bodySize = new TextEncoder().encode(requestBody).length;

          // axios.post(baseUrl,postData,{
          //   headers:{
          //     "x-log-apiversion": "0.6.0" ,
          //     "x-log-bodyrawsize": bodySize
          //   }
          // })
        },
        userAgree(){
          var that = this
          if (this.agreeClickBtn) {
            this.agreeClickBtn = false          
            this.logAli("agreement_agree",{
              message: "success",
              ua: navigator.userAgent
            },function(){
              console.log("app://agreement?action=1")
              window.location.href = "app://agreement?action=1"
              setTimeout(function(){
                that.agreeClickBtn = true
              }, 3000);
            })
          }
        },
        userReject(){
          var that = this
          if (this.agreeClickBtn) {
            this.agreeClickBtn = false
            
            this.logAli("agreement_web_disagree",{
              message: "disagree"
            },function(){
              console.log("app://agreement?action=0")
              window.location.href = "app://agreement?action=0"
              setTimeout(function(){
                that.agreeClickBtn = true
              }, 3000);
            })
          }
        },
        getLang() {
          var langUrl = 'i18n/'+this.pageCurrentStatus.region+'.json?timeStamp='+Date.now()+'&redirect=1'
          var lang = this.pageCurrentStatus.publish === "cn" ? "CN" : this.pageCurrentStatus.region
          var that = this
          axios.get(langUrl).then(function(res){
            if (res.status == 200) {
              that.label[lang] = res.data
              setTimeout(function(){
                that.$forceUpdate()
              }, 100);
            } 
          }).catch(function(res){
            if(res.response.status == 404) {
              langUrl = 'i18n/ALL.json?timeStamp='+Date.now()+ '&redirect=1'
              axios.get(langUrl).then(function(params){
                if (params.status == 200) {
                  that.label[lang] = params.data
                  setTimeout(function(){
                    that.$forceUpdate()
                  }, 100);
                } 
              })
            }
          });
          console.log('label',this.label)
        },
        $label(string){
          // const str = this.pageCurrentStatus.region + ''
          // console.log('str', str.toLowerCase())
          // var lang = this.pageCurrentStatus.publish === "cn" ? "cn" : str.toLowerCase()
          var lang = this.pageCurrentStatus.publish === "cn" ? "CN" : this.pageCurrentStatus.region
          var messageLable = string.split(".")
          var la
          if(JSON.stringify(this.label) !== "{}") {
            la = !!this.label[lang] ? this.label[lang][messageLable[0]][messageLable[1]]:this.label.ALL[messageLable[0]][messageLable[1]]
          } else {
            la = ''
          }
          // console.log('la',la)
          return la
        },
        errorHander(){
          // error handel
          var that = this
          window.onerror = function(msg, url, lineNo, columnNo, erro){
            var _errToAli = encodeURIComponent(msg+":"+url+":"+lineNo+":"+columnNo)
            var _errorJson = {
              msg:msg,
              url:url,
              lineNo:lineNo,
              columnNo:columnNo
            }
            var errorToSdk = JSON.stringify(_errorJson)
            that.logAli("agreement_error",{
              message: _errToAli
            })            

            // 上报客户端
            var u = navigator.userAgent;
            var isAndroid = u.indexOf('Android') > -1 || u.indexOf('Adr') > -1; //android终端
            var isiOS = !!u.match(/\(i[^;]+;( U;)? CPU.+Mac OS X/); //ios终端
          
            
            if(isiOS && typeof(window.webkit) != undefined){
              window.webkit.messageHandlers['callNativeAPI'].postMessage(['lcx_web_log', errorToSdk]);
            }
          
            if(isAndroid && typeof(jsInterface) != undefined){
              jsInterface.callNativeAPI('lcx_web_log', errorToSdk)
            }

          }
        }      
    }
})
