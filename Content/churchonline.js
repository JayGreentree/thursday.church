  jQuery(function() {
    var days, goLive, hours, intervalId, minutes, seconds;

    // Your churchonline.org url
    var churchUrl = "//web.archive.org/web/20180723010601/http://fchogv.churchonline.org/"

    goLive = function() {
      $("#churchonline_counter .time").hide();
      $("#churchonline_counter .live").show();
    };
    loadCountdown = function(data){
      var seconds_till;
      $("#churchonline_counter").show();
      if (data.response.item.isLive) {
        return goLive();
      } else {
        // Parse ISO 8601 date string
        date = data.response.item.eventStartTime.match(/^(\d{4})-0?(\d+)-0?(\d+)[T ]0?(\d+):0?(\d+):0?(\d+)Z$/)
        dateString = date[2] + "/" + date[3] + "/" + date[1] + " " + date[4] + ":" + date[5] + ":" + date[6] + " +0000"
        seconds_till = ((new Date(dateString)) - (new Date())) / 1000;
        days = Math.floor(seconds_till / 86400);
        hours = Math.floor((seconds_till % 86400) / 3600);
        minutes = Math.floor((seconds_till % 3600) / 60);
        seconds = Math.floor(seconds_till % 60);
        return intervalId = setInterval(function() {
          if (--seconds < 0) {
            seconds = 59;
            if (--minutes < 0) {
              minutes = 59;
              if (--hours < 0) {
                hours = 23;
                if (--days < 0) {
                  days = 0;
                }
              }
            }
          }
          $("#churchonline_counter .days").html((days.toString().length < 2) ? "0" + days : days);
          $("#churchonline_counter .hours").html((hours.toString().length < 2 ? "0" + hours : hours));
          $("#churchonline_counter .minutes").html((minutes.toString().length < 2 ? "0" + minutes : minutes));
          $("#churchonline_counter .seconds").html((seconds.toString().length < 2 ? "0" + seconds : seconds));
          if (seconds === 0 && minutes === 0 && hours === 0 && days === 0) {
            goLive();
            return clearInterval(intervalId);
          }
        }, 1000);
      }
    }
    days = void 0;
    hours = void 0;
    minutes = void 0;
    seconds = void 0;
    intervalId = void 0;
    eventUrl = churchUrl + "/api/v1/events/current"
    msie = /msie/.test(navigator.userAgent.toLowerCase())
    if (msie && window.XDomainRequest) {
        var xdr = new XDomainRequest();
        xdr.open("get", eventUrl);
        xdr.onload = function() {
          loadCountdown(jQuery.parseJSON(xdr.responseText))
        };
        xdr.send();
    } else {
      $.ajax({
        url: eventUrl,
        dataType: "json",
        crossDomain: true,
        success: function(data) {
          loadCountdown(data);
        },
        error: function(xhr, ajaxOptions, thrownError) {
          return console.log(thrownError);
        }
      });
    }
  });
/*
     FILE ARCHIVED ON 01:06:01 Jul 23, 2018 AND RETRIEVED FROM THE
     INTERNET ARCHIVE ON 04:05:34 Aug 18, 2018.
     JAVASCRIPT APPENDED BY WAYBACK MACHINE, COPYRIGHT INTERNET ARCHIVE.

     ALL OTHER CONTENT MAY ALSO BE PROTECTED BY COPYRIGHT (17 U.S.C.
     SECTION 108(a)(3)).
*/
/*
playback timings (ms):
  LoadShardBlock: 167.444 (3)
  esindex: 0.01
  captures_list: 188.68
  CDXLines.iter: 16.187 (3)
  PetaboxLoader3.datanode: 179.234 (5)
  exclusion.robots: 0.296
  exclusion.robots.policy: 0.279
  RedisCDXSource: 1.652
  PetaboxLoader3.resolve: 138.446 (2)
  load_resource: 217.072
*/