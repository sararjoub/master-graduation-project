document.addEventListener('DOMContentLoaded',function(){
 'use strict';const button=document.getElementById('adminSidebarToggle'),side=document.getElementById('adminSidebar');if(button&&side)button.addEventListener('click',()=>queueMicrotask(()=>button.setAttribute('aria-expanded',String(side.classList.contains('open')))));
 function sync(){if(!side)return;const small=matchMedia('(max-width:992px)').matches;side.inert=small&&!side.classList.contains('open');if(button)button.setAttribute('aria-expanded',String(side.classList.contains('open')));}
 function close(){side?.classList.remove('open');document.getElementById('adminSidebarOverlay')?.classList.remove('open');sync();button?.focus();}
 button?.addEventListener('click',()=>queueMicrotask(sync));document.querySelector('[data-admin-close]')?.addEventListener('click',close);document.getElementById('adminSidebarOverlay')?.addEventListener('click',()=>queueMicrotask(sync));document.addEventListener('keydown',e=>{if(e.key==='Escape'&&side?.classList.contains('open'))close();});window.addEventListener('resize',sync);sync();
 const active=side?.querySelector('.admin-sidebar-link.active');if(active?.closest('details'))active.closest('details').open=true;
 document.addEventListener('click',e=>{const b=e.target.closest('button');if(!b)return;const page=location.pathname.split('/').pop();} ,true);
});