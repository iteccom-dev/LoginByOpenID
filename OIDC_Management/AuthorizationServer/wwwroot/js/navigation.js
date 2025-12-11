$(function () {
    LoadLogo();
    //Các nút chuyển trang
    const main_content = $("#content-main");
    $(document).on("click", "#client-list-link", function () {
    
        $.ajax({
            url: '/Admin/Home/ClientList',
            method: 'GET',
            headers: { 'X-Requested-With': 'XMLHttpRequest' }
        })
            .done(function (html) {
                main_content.html(html);

            })
            .fail(function (xhr) {
                main_content.html('<div class="alert alert-danger">' + (xhr.responseText || 'Không tải được chi tiết') + '</div>');

            });


    });
    $(document).on("click", "#user-list-link", function () {
      
        $.ajax({
            url: '/Admin/Home/UserList',
            method: 'GET',
            headers: { 'X-Requested-With': 'XMLHttpRequest' }
        })
            .done(function (html) {
                main_content.html(html);

            })
            .fail(function (xhr) {
                main_content.html('<div class="alert alert-danger">' + (xhr.responseText || 'Không tải được chi tiết') + '</div>');

            });
    });
    $(document).on("click", "#user-create-link", function () {
       
        $.ajax({
            url: '/Admin/Home/UserCreate',
            method: 'GET',
            headers: { 'X-Requested-With': 'XMLHttpRequest' }
        })
            .done(function (html) {
                main_content.html(html);
            })
            .fail(function (xhr) {
                main_content.html('<div class="alert alert-danger">' + (xhr.responseText || 'Không tải được chi tiết') + '</div>');
            });
    });
 
    $(document).on("click", "#setting-session-link", function () {

        $.ajax({
            url: '/Admin/Home/Setting',
            method: 'GET',
            headers: { 'X-Requested-With': 'XMLHttpRequest' }
        })
            .done(function (html) {
                main_content.html(html);
            })
            .fail(function (xhr) {
                main_content.html('<div class="alert alert-danger">' + (xhr.responseText || 'Không tải được chi tiết') + '</div>');
            });
    });


  






});
function LoadLogo() {
    $.ajax({
        url: "/api/get/setlogo",
        type: "GET",
        dataType: "json",
        timeout: 10000,
        success: function (res) {

            if (!res || !res.success || !Array.isArray(res.data)) {
                console.warn("Dữ liệu logo không hợp lệ hoặc trống");
                return;
            }

            const mainLogo = res.data.find(item => item.section === "MainLogo");
            const smallLogo = res.data.find(item => item.section === "SubLogo" || item.section === "Favicon");

            // 🟦 Cập nhật logo NAV
            if (mainLogo?.value) {
                $("#navMainLogo").attr("src", mainLogo.value + "?t=" + new Date().getTime());
            }

            if (smallLogo?.value) {
                $("#navSmallLogo").attr("src", smallLogo.value + "?t=" + new Date().getTime());
            }
        },
        error: function (xhr, status, err) {
            console.error("Lỗi tải logo:", status, err);
        }
    });
}
