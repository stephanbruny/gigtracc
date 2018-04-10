const el = require('crel');
const uiUtils = require('./ui-utils');

module.exports = function (content, header) {
    const overlay = el('div', { class: 'overlay' });
    const modal = el('div', { class: 'modal' });
    const modalContent = el('div', { class: 'modal-content' });

    const modalHeader = el('div', { class: 'modal-header' });
    const closeButton = el('button', { class: 'btn btn-close' }, el('i', { class: 'fas fa-times white' }));

    el(overlay, modal);
    el(modalHeader, closeButton, header);
    el(modal, modalHeader);
    el(modal, modalContent);

    el(modalContent, content);

    closeButton.addEventListener('click', () => overlay.remove());

    return {
        show: () => el(document.body, overlay),
        close: () => overlay.remove(),
        overlay,
        setContent: newContent => {
            uiUtils.clear(modalContent);
            el(modalContent, newContent);
        }
    }
}