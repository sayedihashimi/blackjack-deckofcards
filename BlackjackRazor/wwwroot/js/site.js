// site.js - global small enhancements
(function(){
  function initRoundSummary(){
    const dialog = document.getElementById('round-summary');
    if(!dialog) return;
    // focus title
    const title = dialog.querySelector('#round-summary-title');
    if(title) { title.focus(); }
    // bind close buttons
    dialog.querySelectorAll('[data-roundsummary-close]').forEach(btn => {
      btn.addEventListener('click', () => closeDialog(dialog));
    });
    // auto hide after 8 seconds
    setTimeout(()=> closeDialog(dialog), 8000);
  }

  function closeDialog(el){
    if(!el) return;
    el.setAttribute('aria-hidden','true');
    el.classList.add('opacity-0');
    setTimeout(()=>{ if(el.parentElement) el.remove(); }, 350);
  }

  document.addEventListener('DOMContentLoaded', initRoundSummary);
})();
