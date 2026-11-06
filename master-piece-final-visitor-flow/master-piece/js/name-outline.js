document.addEventListener('DOMContentLoaded',function(){
 'use strict';const reduced=matchMedia('(prefers-reduced-motion: reduce)');
 document.querySelectorAll('.profile-header-card,.welcome-header').forEach(card=>{
  card.classList.add('name-outline-card');const ns='http://www.w3.org/2000/svg',svg=document.createElementNS(ns,'svg');svg.classList.add('name-outline-svg');svg.setAttribute('aria-hidden','true');svg.setAttribute('focusable','false');
  const lines=[0,1].map(i=>{const r=document.createElementNS(ns,'rect');r.setAttribute('class','name-outline-line'+(i?' second':''));svg.append(r);return r;});card.append(svg);let animations=[];
  function update(){animations.forEach(a=>a.cancel());animations=[];const w=card.clientWidth,h=card.clientHeight;svg.setAttribute('viewBox',`0 0 ${w} ${h}`);lines.forEach((line,i)=>{line.setAttribute('x','2');line.setAttribute('y','2');line.setAttribute('width',String(Math.max(0,w-4)));line.setAttribute('height',String(Math.max(0,h-4)));line.setAttribute('rx','22');const length=line.getTotalLength();line.style.strokeDasharray=`${Math.min(140,length*.16)} ${length}`;if(!reduced.matches)animations.push(line.animate([{strokeDashoffset:String(i*length/2)},{strokeDashoffset:String(i*length/2-length)}],{duration:6500,iterations:Infinity,easing:'linear'}));});}
  new ResizeObserver(update).observe(card);reduced.addEventListener('change',update);update();document.addEventListener('visibilitychange',()=>animations.forEach(a=>document.hidden?a.pause():a.play()));
 });
});
