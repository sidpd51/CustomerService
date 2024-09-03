$(document).ready(function () {

    GetCases();
})

function GetCases() {
    $.ajax({
        url: '/Case/GetAllCases',
        type: 'Get',
        dataTypes: 'json',
        success: onSuccess
    })
}

function onSuccess(res) {
    $('#dataTable').DataTable({
        bProcessing: true,
        bLengthChange: true,
        lengthMenu: [[10, 25, 50, -1], [10, 25, 50, "All"]],
        bfilter: true,
        bSort: true,
        bPaginate: true,
        data: res,
        columns: [
            {
                data: 'Title',
                render: function (data, type, row, meta) {
                    return row.title
                }
            },
            {
                data: 'CaseNumber',
                render: function (data, type, row, meta) {
                    return row.caseNumber
                }
            },
            {
                data: 'Priority',
                render: function (data, type, row, meta) {
                    return row.priority
                }
            },
            {
                data: 'Status',
                render: function (data, type, row, meta) {
                    return row.status
                }
            },
            {
                data: 'CreatedOn',
                render: function (data, type, row, meta) {
                    return new Date(row.createdOn).toLocaleDateString("en-GB")
                }
            },
            {
                data: "CaseId",
                render: function (data, type, row, meta) {
                    var isOwner = row.owner === row.userId;
                    var validStatus = row.status !='Problem Solved'&&row.status!='Cancelled';
                    var result = isOwner && validStatus;

                    return (
                        `
                        <div class="w-75 btn-group" role="group">
                            <a href="/Case/Edit/${row.caseId}" class="btn btn-primary mx-2 ${!isOwner ? 'disabled' : ''}" >
                                <i class="bi bi-pencil"></i>
                            </a>
                            <a href="/Case/Delete/${row.caseId}" class="btn btn-danger mx-2 ${!isOwner ? 'disabled' : ''}" >
                                <i class="bi bi-trash"></i>
                            </a>
                            <a href="/Case/Resolve/${row.caseId}" class="btn btn-success mx-2 ${!result ? 'disabled' : ''}" >
                                <i class="bi bi-arrow-up-circle-fill"></i>
                            </a>
                            <a href="/Case/Cancel/${row.caseId}" class="btn btn-warning mx-2 ${!result ? 'disabled' : ''}" >
                                <i class="bi bi-arrow-down-circle-fill"></i>
                            </a>
                        </div>
                        `
                    )
                },
                orderable: false,
            },
        ]
    })
}


