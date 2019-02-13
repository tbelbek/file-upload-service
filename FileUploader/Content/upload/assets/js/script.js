$(function () {
    $("footer").hide();

    if (typeof Cookies.get('UserSession') === 'undefined') {
        $.get("/Home/CreateSessionCookie", function (data) {
            Cookies.set('UserSession', data);
            console.log("Cookie set for user.");
        });
    }

    var ul = $('#upload ul');

    $('.tooltip').tooltipster({
        content: $('#tooltip_content'),
        contentCloning: true,
        theme: ['tooltipster-borderless'],
        functionPosition: function (instance, helper, position) {
            position.coord.top += -40;
            position.coord.left += -100;
            return position;
        }
    });

    $('#drop a').click(function () {
        // Simulate a click on the file input button
        // to show the file browser dialog
        $(this).parent().find('input').click();
    });

    // Initialize the jQuery File Upload plugin
    $('#upload').fileupload({

        beforeSend: function (xhr, data) {
            var cookieData = Cookies.get('UserSession');
            xhr.setRequestHeader('UserSessionCookie', cookieData);
        },
        // This element will accept file drag/drop uploading
        dropZone: $('#drop'),

        // This function is called when a file is added to the queue;
        // either via the browse button, or via drag/drop:
        add: function (e, data) {

            var tpl = $('<li class="working"><input type="text" value="0" data-width="48" data-height="48"' +
                ' data-fgColor="#0788a5" data-readOnly="1" data-bgColor="#3e4043" /><p></p><span><i class="fa fa-circle-o-notch fa-spin fa-white" style="font-size:24px"></i></span></li>');

            // Append the file name and file size
            tpl.find('p').text(data.files[0].name)
                .append('<i>' + formatFileSize(data.files[0].size) + '</i>');

            // Add the HTML to the UL element
            data.context = tpl.appendTo(ul);

            // Initialize the knob plugin
            tpl.find('input').knob();

            // Listen for clicks on the cancel icon
            tpl.find('span').click(function () {

                if (tpl.hasClass('working')) {
                    jqXHR.abort();
                }

                tpl.fadeOut(function () {
                    tpl.remove();
                });

            });

            // Automatically upload the file once it is added to the queue
            var jqXHR = data.submit();
        },

        progress: function (e, data) {

            // Calculate the completion percentage of the upload
            var progress = parseInt(data.loaded / data.total * 100, 10);

            // Update the hidden input field and trigger a change
            // so that the jQuery knob plugin knows to update the dial
            data.context.find('input').val(progress).change();

            if (progress == 100) {
                data.context.removeClass('working');
            }
        },

        fail: function (e, data) {
            // Something has gone wrong!
            data.context.addClass('error');
        },

        done: function (e, data) {
            $(".qrcode_content").attr("src", data.result.QrCode);
            $("#download-link").attr("href", data.result.Url);

            addthis.update('share', 'url', data.result.Url);
            addthis.url = data.result.Url;
            addthis.toolbox(".addthis_inline_share_toolbox");

            $("#link-address").html(data.result.Url.replace(/(^\w+:|^)\/\//, ''));
            $("footer").show();
            $("footer").css("background-color", "#003C00");
            $('.tooltip').tooltipster('content', $('#tooltip_content'));
            $('.tooltip').tooltipster('open');

            setTimeout(function () { $("footer").css("background-color", "#373a3d"); }, 3000);
        }

    });

    $("footer").click(function () {
        $('.tooltip').tooltipster('close');
    });

    // Prevent the default action when a file is dropped on the window
    $(document).on('drop dragover', function (e) {
        e.preventDefault();
    });

    // Helper function that formats the file sizes
    function formatFileSize(bytes) {
        if (typeof bytes !== 'number') {
            return '';
        }

        if (bytes >= 1000000000) {
            return (bytes / 1000000000).toFixed(2) + ' GB';
        }

        if (bytes >= 1000000) {
            return (bytes / 1000000).toFixed(2) + ' MB';
        }

        return (bytes / 1000).toFixed(2) + ' KB';
    }

});