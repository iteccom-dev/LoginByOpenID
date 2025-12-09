
$(document).ready(function () {
    loadSetTime();
    $(document).off("click", "#btnUpdateSettingTime").on("click", "#btnUpdateSettingTime", function () {

        let sTime = $("#sessionTime").val();
        let rtTime = $("#refreshTokenTime").val();

        $.ajax({
            url: "/api/settime",
            type: "POST",
            data: {
                sTime: sTime,
                rtTime: rtTime
            },
            success: function (res) {
                if (res.success) {
                    toastr.success(res.message);
                    loadSetTime();
                } else {
                    toastr.error(res.message);
                }

            },
            error: function (xhr) {
                toastr.error("Lỗi kết nối server");
            }
        });
       
    });
});
function loadSetTime() {
    $.ajax({
        url: "/api/get/settime",   // Đổi thành API của bạn
        type: "GET",
        success: function (res) {

            if (res.success && res.data) {

                // Lặp từng item
                res.data.forEach(item => {
                    if (item.name === "SetSessionTime") {
                        $("#sessionTime").val(item.value);
                    }

                    if (item.name === "SetTokenTime") {
                        $("#refreshTokenTime").val(item.value);
                    }
                });
            }
        },
        error: function () {
            console.log("Không thể kết nối server");
        }
    });

}