(function () { 

    var ddl = $('.navbar .user ul').hide();
    var avatar = $('.navbar .user .avatar');
$('.navbar .user .avatar').click(function (e) {
    
    ddl.toggle();
    e.preventDefault();
});


$(document).click(function (e) {
    if (e.target != avatar[0] && e.target.parentNode != avatar[0]) {
        ddl.hide();
    }
    //console.log('clicked off');
});
})();