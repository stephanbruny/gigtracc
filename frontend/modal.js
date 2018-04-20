const el = require('crel');
const uiUtils = require('./ui-utils');

module.exports = function (content, header, modalOpts, closeOnClickOutside, closeOnEscape) {
    const modalDefaultAttributes = { class: 'modal' };
    const modalAttributes = modalOpts ? Object.assign(modalDefaultAttributes, modalOpts) : modalDefaultAttributes;
    const overlay = el('div', { class: 'overlay' });
    const modal = el('div', modalAttributes);
    const modalContent = el('div', { class: 'modal-content' });

    const modalHeader = el('div', { class: 'modal-header' });
    const closeButton = el('button', { class: 'btn btn-close' }, el('i', { class: 'fas fa-times white' }));

    el(overlay, modal);
    el(modalHeader, closeButton, header);
    el(modal, modalHeader);
    el(modal, modalContent);

    el(modalContent, content);

    closeButton.addEventListener('click', () => overlay.remove());

    const closeModal = () => overlay.remove();

    if (closeOnClickOutside) {
        modal.addEventListener('click', ev => ev.stopPropagation());
        overlay.addEventListener('click', ev => {
            closeModal();
        });
    }

    if (closeOnEscape) {
        document.addEventListener('keydown', ev => {
            if (ev.key === "Escape") closeModal();
        });
    }

    return {
        show: () => {
            el(document.body, overlay);
            modal.focus();
            return overlay;
        },
        close: closeModal,
        overlay,
        setContent: newContent => {
            uiUtils.clear(modalContent);
            el(modalContent, newContent);
        }
    }
}