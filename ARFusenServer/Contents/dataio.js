/// <reference path="setting.js" />

var dataio = new function () {

    function goToLogin() {
        location.href = setting.contentRoot + "login.html";
    }

    /**
     * @param {function(object, string)} callback
     */
    this.getList = function (callback) {
        var param = {
            type: "GET",
            url: setting.apiRoot + "ArValue/getList",
            authorization: account.getToken(),
            callback: ajaxCallback,
        }

        function ajaxCallback(event) {
            if (event.status == 401) goToLogin();

            if (event.success) {
                var data = JSON.parse(event.response);
                callback(data, "");
            } else {
                callback(null, event.message);
            }
        }
        common.ajax.ajax(param)
    }
}