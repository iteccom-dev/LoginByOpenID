$(function () {

    //Các nút chuyển trang
    const main_content = $("#content-main");
    $("#client-list-link").on("click", function () {
    
        $.ajax({
            url: '/Home/ClientList',
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
    $("#user-list-link").on("click", function () {
      
        $.ajax({
            url: '/Home/UserList',
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
            url: '/Home/UserCreate',
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