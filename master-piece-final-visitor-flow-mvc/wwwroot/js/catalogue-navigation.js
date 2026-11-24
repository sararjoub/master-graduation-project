/* Navigation only. Existing category cards, modals, and CSS stay unchanged. */
document.addEventListener('DOMContentLoaded',function(){
 'use strict';
 const data={education:['languages','tutoring'],technology:['web-development','ui-ux'],handmade:['crafts','photography'],entertainment:['music','performance'],business:['marketing','management'],'health-fitness':['fitness','wellness']};
 const cards=document.querySelectorAll('.category-card');
 cards.forEach(card=>{
  if(card.dataset.name==='technology')return;
  const title=card.querySelector('.category-name')?.textContent.trim().toLowerCase()||'';
  const key=title.includes('health')?'health-fitness':Object.keys(data).find(k=>title.includes(k));if(!key)return;
  card.href='category-overview.html?category='+encodeURIComponent(key);
 });
});
