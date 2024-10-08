﻿$(document).ready(function () {
    $('#dataTable').DataTable(
        {
            ajax: {
                url: "/Case/GetAllCases",
                type: "POST",
            },
            processing: true,
            serverSide: true,
            filter: true,
            columns: [
                { data: "title", name: "Title" },
                { data: "caseNumber", name: "TicketNumber" },
                { data: "priority", name: "PriorityCode" },
                { data: "status", name: "StatusCode" },
                {
                    data: "createdOn",
                    name: "CreatedOn",
                    render: function (data, type, row, meta) {
                        return new Date(row.createdOn).toLocaleDateString("en-GB")
                    }
                },
                {
                    data: "caseId",
                    render: function (data, type, row, meta) {
                        var isOwner = row.ownerId === row.userId;
                        var validStatus = row.status != 'Problem Solved' && row.status != 'Cancelled';
                        var result = isOwner && validStatus;

                        return (
                            `
                        <div class="w-75 btn-group" role="group">`+`
                            <a href="/Case/Edit/${row.caseId}" class="btn btn-primary rounded-2 mx-2 ${!result ? 'disabled' : ''}" >
                                <i class="bi bi-pencil"></i>
                            </a>
                            <a href="/Case/Delete/${row.caseId}" class="btn btn-danger rounded-2 mx-2 ${!result ? 'disabled' : ''}" >
                                <i class="bi bi-trash"></i>
                            </a>
                            <a href="/Case/Resolve/${row.caseId}" class="btn btn-success rounded-2 mx-2 ${!result ? 'disabled' : ''}" >
                                <i class="bi bi-arrow-up-circle-fill"></i>
                            </a>
                            <a href="/Case/Cancel/${row.caseId}" class="btn btn-warning rounded-2 mx-2 ${!result ? 'disabled' : ''}" >
                                <i class="bi bi-arrow-down-circle-fill"></i>
                            </a>
                             <a href="/Case/Reactivate/${row.caseId}" class="btn btn-info rounded-2 mx-2 ${!result&&isOwner ? '' : 'disabled'}" >
                                <i class="bi bi-door-open-fill"></i>
                            </a>
                        </div>
                        `
                        )
                    },
                    orderable: false,
                },

            ],
            order: [4,'desc']
        }
    )
})

