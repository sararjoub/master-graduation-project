document.addEventListener('DOMContentLoaded',function(){
 'use strict';
 const categories={
  education:{name:'Education',icon:'🎓',subs:[['mathematics','Mathematics'],['physics','Physics'],['chemistry','Chemistry'],['english','English'],['arabic','Arabic Language']]},
  technology:{name:'Technology',icon:'💻',subs:[['web-development','Web Development'],['ui-ux','UI/UX Design'],['mobile-development','Mobile Development'],['cybersecurity','Cybersecurity']]},
  handmade:{name:'Handmade',icon:'🎨',subs:[['photography','Photography'],['crafts','Crafts'],['painting','Painting']]},
  entertainment:{name:'Entertainment',icon:'🎬',subs:[['music','Music'],['performance','Performance']]},
  business:{name:'Business',icon:'💼',subs:[['marketing','Marketing'],['management','Management']]},
  'health-fitness':{name:'Health & Fitness',icon:'❤️',subs:[['fitness','Fitness'],['wellness','Wellness']]}
 };
 const key=new URLSearchParams(location.search).get('category'),c=categories[key],root=document.getElementById('category-list');
 if(!c){document.getElementById('category-title').textContent='Category not found';root.textContent='Choose a published category from the catalogue.';return;}
 document.getElementById('category-title').textContent=c.name;
 c.subs.forEach(([id,name])=>{const a=document.createElement('a');a.className='category-card';a.href='sub-category.html?cat='+encodeURIComponent(id);const icon=document.createElement('div');icon.className='category-icon';icon.textContent=c.icon;const h=document.createElement('h3');h.className='category-name';h.textContent=name;const p=document.createElement('p');p.className='category-description';p.textContent='Explore '+name+' skills';a.append(icon,h,p);root.append(a);});
});
