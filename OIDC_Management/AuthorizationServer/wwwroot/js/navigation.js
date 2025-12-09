$(function () {

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