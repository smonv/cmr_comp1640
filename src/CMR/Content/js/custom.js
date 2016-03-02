(function($) {
  $(document).ready(function() {
    $('#cssmenu li.has-sub>a').on('click', function() {
      $(this).removeAttr('href');
      var element = $(this).parent('li');
      if (element.hasClass('open')) {
        element.removeClass('open');
        element.find('li').removeClass('open');
        element.find('ul').slideUp(200);
      } else {
        element.addClass('open');
        element.children('ul').slideDown(200);
        element.siblings('li').children('ul').slideUp(200);
        element.siblings('li').removeClass('open');
        element.siblings('li').find('li').removeClass('open');
        element.siblings('li').find('ul').slideUp(200);
      }
    });
  });

  $(document).ready(function() {
    $('#cssmenu>ul>li>a').on('click', function() {
      var element = $(this).parent('li');
      if(!element.hasClass('has-sub')){
        $('#cssmenu>ul>li').removeClass('open');
        $('#cssmenu>ul>li').find('ul').slideUp(200);
      }
    });
  });

  $('.list-item').on('click', function(){
    $('.list-item').removeClass('active');
    $(this).addClass('active');
  });

})(jQuery);

$('#btn-burger-navbar-left').on('click', function(){
  if($('#nav-menu').hasClass('hide-nav')){
    $('#nav-menu').removeClass('hide-nav');
    $('.container-right').removeClass('show-full');
  }else{
    $('#nav-menu').addClass('hide-nav');
    $('.container-right').addClass('show-full');
  }
});
$('#btn-burger-navbar-left-xs').on('click', function(){
  if($('#nav-menu').hasClass('show-nav')){
    $('#nav-menu').removeClass('show-nav');
    $('.container-right').removeClass('hide-full');
  }else{
    $('#nav-menu').addClass('show-nav');
    $('.container-right').addClass('hide-full');
  }
});
