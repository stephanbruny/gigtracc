const request = require('request-promise');

module.exports = function(entryData) {

    let currentData = Object.assign({}, entryData);

    const modify = field => data => {
        if (data === currentData[field]) return Promise.resolve(currentData);
        return request.put(`http://localhost:8080/api/entries/${entryData.id}/${field}`, {
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
            return request.post('http://localhost:8080/api/entries', {
                body: JSON.stringify({ date, location, duration, description })
            })
        },
        delete: () => request.delete('http://localhost:8080/api/entries/' + entryData.id)
    }

}