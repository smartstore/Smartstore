/*****************************************/
// Name: Javascript Textarea BBCode Markup Editor
// Version: 1.3
// Author: Balakrishnan
// Last Modified Date: 25/jan/2009
// License: Free
// URL: http://www.corpocrat.com
/******************************************/

var textarea;
var content;
var webRoot;

function edToolbar(obj) {
    document.write("<div class=\"bb-toolbar btn-toolbar mb-1\">");
    document.write("<button type='button' class='btn btn-plain btn-icon btn-sm' name='btnBold' aria-label='Bold' onClick=\"doAddTags('[b]','[/b]','" + obj + "')\"><sm-icon name='bold' aria-hidden='true'></sm-icon></button>");
    document.write("<button type='button' class='btn btn-plain btn-icon btn-sm' name='btnItalic' aria-label='Italic' onClick=\"doAddTags('[i]','[/i]','" + obj + "')\"><sm-icon name='italic' aria-hidden='true'></sm-icon></button>");
    document.write("<button type='button' class='btn btn-plain btn-icon btn-sm' name='btnUnderline' aria-label='Underline' onClick=\"doAddTags('[u]','[/u]','" + obj + "')\"><sm-icon name='underline' aria-hidden='true'></sm-icon></button>");
    document.write("<button type='button' class='btn btn-plain btn-icon btn-sm' name='btnLink' aria-label='Link' onClick=\"doURL('" + obj + "')\"><sm-icon name='link' aria-hidden='true'></sm-icon></button>");
    document.write("<button type='button' class='btn btn-plain btn-icon btn-sm' name='btnCode' aria-label='Code' onClick=\"doAddTags('[code]','[/code]','" + obj + "')\"><sm-icon name='code' aria-hidden='true'></sm-icon></button>");
    document.write("<button type='button' class='btn btn-plain btn-icon btn-sm' name='btnQuote' aria-label='Quote' onClick=\"doAddTags('[quote]','[/quote]','" + obj + "')\"><sm-icon name='quotes' aria-hidden='true'></sm-icon></button>");
    document.write("</div>");
}

function doURL(obj) {
    textarea = document.getElementById(obj);
    var url = prompt('Enter the URL:', 'http://');
    var scrollTop = textarea.scrollTop;
    var scrollLeft = textarea.scrollLeft;
    if (url != '' && url != null) {
        if (document.selection) {
            textarea.focus();
            var sel = document.selection.createRange();
            if (sel.text == "") {
                sel.text = '[url]' + url + '[/url]';
            }
            else {
                sel.text = '[url=' + url + ']' + sel.text + '[/url]';
            }
        }
        else {
            var len = textarea.value.length;
            var start = textarea.selectionStart;
            var end = textarea.selectionEnd;

            var sel = textarea.value.substring(start, end);

            if (sel == "") {
                var rep = '[url]' + url + '[/url]';
            }
            else {
                var rep = '[url=' + url + ']' + sel + '[/url]';
            }

            textarea.value = textarea.value.substring(0, start) + rep + textarea.value.substring(end, len);
            textarea.scrollTop = scrollTop;
            textarea.scrollLeft = scrollLeft;
        }
    }
}

function doAddTags(tag1, tag2, obj) {
    textarea = document.getElementById(obj);
    // Code for IE
    if (document.selection) {
        textarea.focus();
        var sel = document.selection.createRange();
        sel.text = tag1 + sel.text + tag2;
    }
    else {  // Code for Mozilla Firefox
        var len = textarea.value.length;
        var start = textarea.selectionStart;
        var end = textarea.selectionEnd;
        var scrollTop = textarea.scrollTop;
        var scrollLeft = textarea.scrollLeft;
        var sel = textarea.value.substring(start, end);
        var rep = tag1 + sel + tag2;
        textarea.value = textarea.value.substring(0, start) + rep + textarea.value.substring(end, len);
        textarea.scrollTop = scrollTop;
        textarea.scrollLeft = scrollLeft;
    }
}