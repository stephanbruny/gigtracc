require('moment/locale/de');
const moment = require('moment');
const el = require('crel');
const Project = require('./view/project');
const request = require('request-promise');
const url = require('./model/url');
const modal = require('./modal');
const uiUtils = require('./ui-utils');
const Nanocal = require('nanocal');
const widgets = require('./widgets');
const JSONEditor = require('jsoneditor');

const projectEdit = require('./view/project-edit');

const buildProjectPage = (parent) => (month) => {
    const projectsDate = moment().month(month);
    return request.get(url('api', 'projects', projectsDate.format('YYYY-MM')), { json: true })
    .then(projects => Promise.all(projects.map(proj => Project(parent)(proj, month))))
    .then(() => {
        const projectTools = el('div', { class: 'project-tools' });
        const addProjectButton = el('button', { class: 'btn white hover-shadow ' }, el('span', {class: 'fa fa-plus fa-2x'}));

        addProjectButton.addEventListener('click', () => {
            const container = el('div', { class: 'form' });
            const projectModal = modal(container, 'New Project');
            const prom = projectEdit(container);
            projectModal.show();
            prom
            .then(data =>
                request.post(url('api/project'), { json: true, body: data })
            ).then(() => projectModal.close())
        })

        el(projectTools, addProjectButton);
        el(page, projectTools);
    });
}

const getHeaderTools = () => {
    const headerTools = el('div', { class: 'tools' });

    const eventSourceButton = el('button', { class: 'btn' }, el('span', { class: 'white fas fa-book fa-2x' }));
    eventSourceButton.addEventListener('click', () => {
        const container = el('div');
        const editorModal = modal(container, 'Event Source');
        const editor = new JSONEditor(container);
        editorModal.show();
        request.get(url('api/event-stream'))
            .then(JSON.parse)
            .then(data => data.map(item => Object.assign(item, { content: JSON.parse(item.content) })))
            .then(data => {
                return data.map(item => {
                    if (item.content && item.content.Modified) {
                        return Object.assign(item, { content: { Modified: JSON.parse(item.content.Modified[1]) } })
                    }
                    return item;
                })
            })
            .then(data => editor.set(data))

    })

    el(headerTools, eventSourceButton);

    return headerTools;
}

(function() {
    const page = el('div', { id: 'page' });

    const header = el('div', { id: 'header' });

    el(document.body, header, page);

    const buildPage = buildProjectPage(page);

    const showProjects = month => {
        const prevMonthButton = el('button', { class:'btn white' }, el('span', { class: 'fa fa-chevron-left fa-2x' }));
        const nextMonthButton = el('button', { class:'btn white' }, el('span', { class: 'fa fa-chevron-right fa-2x' }));

        uiUtils.clear(header);

        const headerTitle = el('h1', moment().month(month).format('MMMM YYYY'));
        el(header, prevMonthButton, headerTitle, nextMonthButton);
        uiUtils.clear(page);
        return buildPage(month)
            .then(() => new Promise((resolve) => {
                prevMonthButton.addEventListener('click', () => resolve(showProjects(month - 1)));
                nextMonthButton.addEventListener('click', () => resolve(showProjects(month + 1)));
            }));
    }

    showProjects(moment().month());


})();