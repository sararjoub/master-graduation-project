
document.addEventListener('DOMContentLoaded', () => {

    // 1. منطق التنقل بين الخطوات
    window.goToStep = (stepNumber) => {
        // إخفاء كل الخطوات
        document.querySelectorAll('.setup-step').forEach(step => step.classList.remove('active'));
        // إظهار الخطوة المطلوبة
        document.getElementById(`step-${stepNumber}`).classList.add('active');

        // تحديث شريط التقدم
        document.querySelectorAll('.progress-step').forEach((step, index) => {
            let stepNum = index + 1;
            step.classList.remove('active', 'completed');
            if (stepNum < stepNumber) {
                step.classList.add('completed');
                step.querySelector('.progress-circle').textContent = '✓';
            } else if (stepNum === stepNumber) {
                step.classList.add('active');
            }
        });
        
        // إعادة تعيين النص للخطوة النشطة
        let activeCircle = document.querySelector(`.progress-step[data-step="${stepNumber}"] .progress-circle`);
        if (activeCircle) activeCircle.textContent = stepNumber;
    };

    // 2. اختيار المهارات (Chips)
    document.querySelectorAll('.skill-chip').forEach(chip => {
        chip.addEventListener('click', () => {
            chip.classList.toggle('selected');
        });
    });

    


    // 4. إنهاء الإعداد
    window.finishSetup = (btn) => {
        if (!document.querySelector("#selectedLearnSkills .selected-skill-tag")) { alert("Choose at least one learning interest before continuing."); return; }
        btn.innerHTML = 'Saving...';
        btn.disabled = true;
        btn.style.opacity = '0.7';
        document.getElementById('profileSetupForm').submit();
    };

    // 5. Avatar Upload Logic

    let avatarInput = document.getElementById('avatarInput');
    let uploadBtn = document.getElementById('uploadBtn');
    let avatarPreview = document.getElementById('avatarPreview');

    // عندما يضغط المستخدم على زر Upload Photo
    if (uploadBtn) {
        uploadBtn.addEventListener('click', () => {
            avatarInput.click(); // فتح مستكشف الملفات
        });
    }

    // عندما يختار المستخدم صورة
    if (avatarInput) {
        avatarInput.addEventListener('change', (e) => {
            let file = e.target.files[0];
            
            if (file) {
                // التحقق من حجم الصورة (مثلاً أقل من 2 ميجا)
                if (!["image/jpeg", "image/png", "image/webp"].includes(file.type) || file.size > 2 * 1024 * 1024) {
                    alert('Choose a JPEG, PNG, or WebP image under 2MB.');
                    return;
                }

                // استخدام FileReader لقراءة الصورة
                let reader = new FileReader();
                
                reader.onload = (event) => {
                    // تحديث الـ Avatar بالصورة الجديدة
                    avatarPreview.innerHTML = `<img src="${event.target.result}" alt="Profile Picture">`;
                    avatarPreview.classList.add('has-image'); // إضافة كلاس لتغيير الستايل
                };
                
                reader.readAsDataURL(file); // قراءة الصورة كـ Base64
            }
        });
    }
        
    // قائمة المهارات (Mock Data)

   let allSkills = Array.isArray(window.profileSkills) ? window.profileSkills : [];

// دالة إعداد البحث الذكي
function setupSmartSearch(inputId, resultsId, selectedContainerId, inputName) {
    let input = document.getElementById(inputId);
    let results = document.getElementById(resultsId);
    let selectedContainer = document.getElementById(selectedContainerId);

    if (!input || !results) return;

    // 1. البحث الفوري عند الكتابة
    input.addEventListener('input', (e) => {
        let query = e.target.value.toLowerCase().trim();
        results.innerHTML = '';

        if (query.length === 0) {
            results.classList.add('hidden');
            return;
        }

        // فلترة المهارات
        let filtered = allSkills.filter(skill => 
            window.SitePrefs.matches(`${skill.name} ${skill.category}`, query)
        );

        if (filtered.length === 0) {
            results.innerHTML = '<div class="search-no-results">No skills found</div>';
            results.classList.remove('hidden');
            return;
        }

        // عرض النتائج
        filtered.forEach(skill => {
            let item = document.createElement('div');
            item.className = 'search-result-item';
            item.textContent = `${skill.name} · ${skill.category}${skill.tier === 'Plus' ? ' · Plus' : ''}`;
            item.addEventListener('click', () => {
                addSelectedSkill(skill, selectedContainer, inputName);
                input.value = '';
                results.classList.add('hidden');
            });
            results.appendChild(item);
        });

        results.classList.remove('hidden');
    });

    // 2. إظهار اقتراحات عند النقر على الحقل (Focus)
    input.addEventListener('focus', () => {
        if (input.value.trim().length === 0) {
            results.innerHTML = '';
            // عرض جميع المهارات الفعلية القادمة من قاعدة البيانات
            allSkills.forEach(skill => {
                let item = document.createElement('div');
                item.className = 'search-result-item';
                item.textContent = `${skill.name} · ${skill.category}`;
                item.addEventListener('click', () => {
                    addSelectedSkill(skill, selectedContainer, inputName);
                    input.value = '';
                    results.classList.add('hidden');
                });
                results.appendChild(item);
            });
            results.classList.remove('hidden');
        }
    });

    // 3. إخفاء النتائج عند النقر خارجها
    document.addEventListener('click', (e) => {
        if (!input.contains(e.target) && !results.contains(e.target)) {
            results.classList.add('hidden');
        }
    });
}

// دالة إضافة المهارة المختارة
function addSelectedSkill(skill, container, inputName) {
    // التحقق من عدم التكرار
    let existing = container.querySelector(`[data-skill-id="${skill.id}"]`);
    if (existing) return;

    let tag = document.createElement('div');
    tag.className = 'selected-skill-tag';
    tag.dataset.skillId = skill.id;
    tag.innerHTML = `
        <span>${skill.name}</span>
        <input type="hidden" name="${inputName}" value="${skill.id}">
        <button type="button" class="remove-skill-btn">✕</button>
    `;
    
    tag.querySelector('.remove-skill-btn').addEventListener('click', () => {
        tag.remove();
    });

    container.appendChild(tag);
}

// تفعيل البحث في الخطوتين
setupSmartSearch('teachSkillSearch', 'teachSearchResults', 'selectedTeachSkills', 'TeachSkillIds');
setupSmartSearch('learnSkillSearch', 'learnSearchResults', 'selectedLearnSkills', 'LearnSkillIds');
});
