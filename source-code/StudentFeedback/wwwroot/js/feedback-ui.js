'use strict';
// Desktop/mobile navigation: keep the active route visible in the horizontal menu.
document.querySelectorAll('.side-menu').forEach(menu=>{
 const active=menu.querySelector('.active');
 if(active && window.innerWidth<900) active.scrollIntoView({block:'nearest',inline:'nearest'});
});
