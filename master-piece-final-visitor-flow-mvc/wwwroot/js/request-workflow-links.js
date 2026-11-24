document.addEventListener('DOMContentLoaded',function(){
 'use strict';

 document.querySelectorAll('.review-stars-input').forEach(root=>{const a=document.createElement('a');a.href='session-review.html';a.className='btn btn-outline btn-sm';a.textContent='Write a review';root.insertAdjacentElement('afterend',a);});
});
