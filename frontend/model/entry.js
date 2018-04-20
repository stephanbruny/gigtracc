const request = require('request-promise');
const url = require('./url');

module.exports = project => entryData => {

    let currentData = Object.assign({}, entryData);

    const modify = field => data => {
        if (data === currentData[field]) return Promise.resolve(currentData);
        return request.put(url('api', 'project', project.id, 'entry', entryData.id, field), {
            body: data
        })
        .then(() => {
            currentData[field] = data;
            return currentData;
        });
    }

    return {
        getData: () => currentData,
        modifyDescription: modify('description'),
        modifyDuration: modify('duration'),
        modifyLocation: modify('location'),
        create: (date, location, duration, description) => {
            return request.post(url('api', 'project','entry'), {
                body: JSON.stringify({ projectId: project.id, date, location, duration, description })
            })
        },
        delete: () => request.delete(url('api', 'entries', entryData.id))
    }

}