const request = require('request-promise');
const moment = require('moment');
const url = require('./url');

module.exports = project => {
    const formatDate = date => moment(date).format('YYYY-MM-DD');

    const getProjectModelData = data => {
        return Object.assign(
            {
                clientName: data.client.name,
                clientAddress: data.client.address,
                pricePerHour: data.pricePerHour || data.payment.pricePerHour,
                tax: data.tax || data.payment.tax
            },
            data,
            { startDate: formatDate(data.startDate), endDate: formatDate(data.endDate) }
        );
    }

    let currentData = project ? getProjectModelData(project) : {};
    console.log("Project Model", currentData)

    const modify = field => data => {
        console.log("modify", field, currentData[field])
        if (data === currentData[field]) return Promise.resolve(currentData);
        return request.put(url('api', 'project', project.id, field), {
            body: data
        })
        .then(() => {
            currentData[field] = data;
            currentData = getProjectModelData(currentData);
            return currentData;
        });
    }

    const update = data => {
        const updateData = getProjectModelData(data);
        return Promise.all(
            Object.keys(updateData)
            .filter(key => !['client'].includes(key))
            .map(key => modify(key)(updateData[key]))
        );
    }

    return {
        getData: () => currentData,
        modifyName: modify('name'),
        modifyDescription: modify('description'),
        modifyStartDate: modify('startDate'),
        modifyEndDate: modify('endDate'),
        modifyClientName: modify('clientName'),
        modifyClientAddress: modify('clientName'),
        modifyPaymentPrice: modify('pricePerHour'),
        modifyPaymentTax: modify('tax'),
        update: update,
        create: (data) => {
            return request.post(url('api', 'project'), {
                body: JSON.stringify(data)
            })
        },
        delete: () => request.delete(url('api', 'project', entryData.id))
    }

}