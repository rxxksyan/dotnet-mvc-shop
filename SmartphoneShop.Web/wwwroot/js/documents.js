function downloadDocument(elementId, filename) {
    var el = document.getElementById(elementId);
    if (!el) return;

    var btn = event.currentTarget;
    btn.disabled = true;
    btn.textContent = 'Скачивание...';

    el.style.position = 'absolute';
    el.style.left = '-9999px';
    el.style.top = '0';
    el.style.display = 'block';

    html2canvas(el, {
        scale: 3,
        backgroundColor: '#1a1a1a',
        allowTaint: false,
        useCORS: true,
        logging: false
    }).then(function(canvas) {
        var imgData = canvas.toDataURL('image/jpeg', 0.97);
        var jsPDF = window.jspdf.jsPDF;
        var doc = new jsPDF('p', 'mm', 'a4');
        var pageWidth = doc.internal.pageSize.getWidth();
        var pageHeight = doc.internal.pageSize.getHeight();
        var usableWidth = pageWidth;
        var imgHeight = (canvas.height * usableWidth) / canvas.width;
        var heightLeft = imgHeight;
        var position = 0;

        doc.setFillColor(26, 26, 26);
        doc.rect(0, 0, pageWidth, pageHeight, 'F');
        doc.addImage(imgData, 'JPEG', 0, 0, usableWidth, imgHeight);
        heightLeft -= pageHeight;

        while (heightLeft > 0) {
            position = -(imgHeight - heightLeft);
            doc.addPage();
            doc.setFillColor(26, 26, 26);
            doc.rect(0, 0, pageWidth, pageHeight, 'F');
            doc.addImage(imgData, 'JPEG', 0, position, usableWidth, imgHeight);
            heightLeft -= pageHeight;
        }

        doc.save(filename);
        el.style.position = '';
        el.style.left = '';
        el.style.top = '';
        el.style.display = 'none';
        btn.disabled = false;
        btn.innerHTML = btn.getAttribute('data-original-text') || btn.textContent;
    }).catch(function(err) {
        console.error('PDF generation failed:', err);
        el.style.position = '';
        el.style.left = '';
        el.style.top = '';
        el.style.display = 'none';
        btn.disabled = false;
        btn.innerHTML = btn.getAttribute('data-original-text') || btn.textContent;
        alert('Не удалось сгенерировать PDF. Попробуйте ещё раз.');
    });
}

function generateRepairReceipt(repairId, data) {
    var existing = document.getElementById('repairReceiptTemp');
    if (existing) existing.remove();

    var isWarrantyNoFault = data.isWarranty && !data.isClientFault;

    var partsHtml = '';
    var partsTotal = 0;
    if (data.parts && data.parts.length > 0) {
        data.parts.forEach(function(p) {
            partsTotal += p.price;
            var statusText = p.isAvailable ? 'В наличии' : 'Ожидание ~' + (p.waitDays || '?') + ' дн.';
            var statusColor = p.isAvailable ? '#4CAF50' : '#D44177';
            partsHtml += '<div style="display:flex; justify-content:space-between; align-items:center; padding:10px 0; border-bottom:1px solid #2c2c2c;">' +
                '<span style="color:#f0f0f0; font-size:14px;">' + p.name + '</span>' +
                '<div style="display:flex; align-items:center; gap:10px;">' +
                '<span style="color:' + statusColor + '; font-size:12px; padding:2px 8px; border-radius:4px; background:' + (p.isAvailable ? 'rgba(76,175,80,0.1)' : 'rgba(212,65,119,0.1)') + ';">' + statusText + '</span>' +
                '<span style="color:#D44177; font-size:14px; font-weight:600; min-width:80px; text-align:right;">' + p.price.toFixed(2) + ' BYN</span>' +
                '</div></div>';
        });
    } else if (isWarrantyNoFault) {
        partsHtml = '<p style="color:#4CAF50; font-size:13px; margin:8px 0;">Ремонт по гарантии — без замены запчастей</p>';
    } else {
        partsHtml = '<p style="color:#888; font-size:13px; margin:8px 0;">Не указаны</p>';
    }

    var servicePrice = isWarrantyNoFault ? 0 : (data.servicePrice || 0);
    var total = isWarrantyNoFault ? 0 : (partsTotal + servicePrice);

    var html = '<div id="repairReceiptTemp" style="position:absolute; left:-9999px; top:0; width:800px; padding:50px; background:#1a1a1a; font-family:\'Montserrat\', sans-serif; color:#f0f0f0;">' +
        '<div style="text-align:center; margin-bottom:35px; padding-bottom:20px; border-bottom:2px solid #D44177;">' +
        '<h1 style="font-size:28px; color:#D44177; margin:0 0 5px 0; letter-spacing:2px;">RXXMRKT</h1>' +
        '<p style="font-size:13px; color:#888; margin:0 0 8px 0; letter-spacing:1px;">Ремонт смартфонов</p>';

    if (isWarrantyNoFault) {
        html += '<p style="font-size:14px; color:#4CAF50; margin:0 0 8px 0; padding:6px 16px; background:rgba(76,175,80,0.1); border:1px solid rgba(76,175,80,0.3); border-radius:20px; display:inline-block; font-weight:600;">По гарантии</p>';
    }

    html += '<p style="font-size:16px; color:#f0f0f0; margin:0; font-weight:600;">Акт выполненных работ</p>' +
        '<p style="font-size:14px; color:#888; margin:5px 0 0 0;">№' + repairId + ' от ' + new Date().toLocaleDateString('ru-RU') + '</p>' +
        '</div>' +

        '<div style="padding:20px; background:#252525; border-radius:10px; margin-bottom:25px;">' +
        '<div style="font-size:14px;">' +
        '<div style="display:flex; margin-bottom:8px;"><span style="color:#888; min-width:130px; flex-shrink:0;">Устройство:</span><span style="color:#f0f0f0;">' + data.model + '</span></div>' +
        (data.serialNumber ? '<div style="display:flex; margin-bottom:8px;"><span style="color:#888; min-width:130px; flex-shrink:0;">Серийный номер:</span><span style="color:#f0f0f0;">' + data.serialNumber + '</span></div>' : '') +
        '<div style="display:flex; margin-bottom:8px;"><span style="color:#888; min-width:130px; flex-shrink:0;">Проблема:</span><span style="color:#f0f0f0;">' + data.issue + '</span></div>' +
        '<div style="display:flex;"><span style="color:#888; min-width:130px; flex-shrink:0;">Дата:</span><span style="color:#f0f0f0;">' + new Date().toLocaleDateString('ru-RU') + '</span></div>' +
        '</div></div>';

    if (data.adminNotes) {
        html += '<div style="padding:14px 18px; background:rgba(212,65,119,0.08); border-left:3px solid #D44177; border-radius:8px; margin-bottom:25px;">' +
            '<p style="margin:0; font-size:13px; color:#f0f0f0;"><strong>Сообщение мастера:</strong></p>' +
            '<p style="margin:6px 0 0 0; font-size:13px; color:#b0b0b0;">' + data.adminNotes + '</p>' +
            '</div>';
    }

    if (data.notesForClient) {
        html += '<div style="padding:14px 18px; background:rgba(255,255,255,0.03); border-left:3px solid #888; border-radius:8px; margin-bottom:25px;">' +
            '<p style="margin:0; font-size:13px; color:#f0f0f0;"><strong>Доп. информация:</strong></p>' +
            '<p style="margin:6px 0 0 0; font-size:13px; color:#b0b0b0;">' + data.notesForClient + '</p>' +
            '</div>';
    }

    if (isWarrantyNoFault) {
        html += '<div style="padding:14px 18px; background:rgba(76,175,80,0.08); border-left:3px solid #4CAF50; border-radius:8px; margin-bottom:25px;">' +
            '<p style="margin:0; font-size:13px; color:#b0b0b0;">Ремонт выполнен по гарантии. Стоимость: <strong style="color:#4CAF50;">0 BYN</strong></p>' +
            '<p style="margin:6px 0 0 0; font-size:12px; color:#888;">Гарантийное обслуживание — оплата не требуется</p>' +
            '</div>';
    }

    html += '<div style="margin-bottom:25px;">' +
        '<h3 style="font-size:16px; color:#f0f0f0; margin:0 0 12px 0; padding-bottom:10px; border-bottom:1px solid #2c2c2c;">Выполненные работы и запчасти</h3>' +
        partsHtml +
        '</div>';

    html += '<div style="padding:18px; background:#252525; border-radius:10px; margin-bottom:25px;">';

    if (isWarrantyNoFault) {
        html += '<div style="display:flex; justify-content:space-between; margin-bottom:8px;"><span style="color:#b0b0b0; font-size:14px;">Ремонт по гарантии:</span><span style="color:#4CAF50; font-size:14px; font-weight:600;">0 BYN</span></div>';
    } else {
        if (data.parts && data.parts.length > 0) {
            html += '<div style="display:flex; justify-content:space-between; margin-bottom:8px;"><span style="color:#b0b0b0; font-size:14px;">Запчасти:</span><span style="color:#f0f0f0; font-size:14px;">' + partsTotal.toFixed(2) + ' BYN</span></div>';
        }
        if (servicePrice > 0) {
            html += '<div style="display:flex; justify-content:space-between; margin-bottom:8px;"><span style="color:#b0b0b0; font-size:14px;">Услуга мастера:</span><span style="color:#f0f0f0; font-size:14px;">' + servicePrice.toFixed(2) + ' BYN</span></div>';
        }
    }

    html += '<div style="display:flex; justify-content:space-between; font-weight:700; font-size:18px; border-top:2px solid #3a3a3a; padding-top:10px; margin-top:8px;">' +
        '<span style="color:#f0f0f0;">Итого к оплате:</span><span style="color:' + (isWarrantyNoFault ? '#4CAF50' : '#D44177') + ';">' + total.toFixed(2) + ' BYN</span></div>';

    html += '</div>';

    if (!isWarrantyNoFault && data.estimatedPrice && data.estimatedPrice > 0) {
        html += '<div style="padding:14px 18px; background:rgba(212,65,119,0.08); border-left:3px solid #D44177; border-radius:8px; margin-bottom:25px;">' +
            '<p style="margin:0; font-size:13px; color:#b0b0b0;">Стоимость диагностики: <strong style="color:#D44177;">' + data.estimatedPrice + ' BYN</strong></p>' +
            '<p style="margin:6px 0 0 0; font-size:12px; color:#888;">При отказе от ремонта оплачивается только диагностика</p>' +
            '</div>';
    }

    html += '<div style="text-align:center; margin-top:35px; padding-top:20px; border-top:1px solid #2c2c2c;">' +
        '<p style="font-size:14px; color:#D44177; margin:0 0 4px 0; font-weight:600;">RXXMRKT</p>' +
        '<p style="font-size:12px; color:#666; margin:0;">Спасибо за обращение!</p>' +
        '</div></div>';

    document.body.insertAdjacentHTML('beforeend', html);

    var el = document.getElementById('repairReceiptTemp');

    html2canvas(el, {
        scale: 3,
        backgroundColor: '#1a1a1a',
        allowTaint: false,
        useCORS: true,
        logging: false
    }).then(function(canvas) {
        var imgData = canvas.toDataURL('image/jpeg', 0.97);
        var jsPDF = window.jspdf.jsPDF;
        var doc = new jsPDF('p', 'mm', 'a4');
        var pageWidth = doc.internal.pageSize.getWidth();
        var pageHeight = doc.internal.pageSize.getHeight();
        var usableWidth = pageWidth;
        var imgHeight = (canvas.height * usableWidth) / canvas.width;
        var heightLeft = imgHeight;
        var position = 0;

        doc.setFillColor(26, 26, 26);
        doc.rect(0, 0, pageWidth, pageHeight, 'F');
        doc.addImage(imgData, 'JPEG', 0, 0, usableWidth, imgHeight);
        heightLeft -= pageHeight;

        while (heightLeft > 0) {
            position = -(imgHeight - heightLeft);
            doc.addPage();
            doc.setFillColor(26, 26, 26);
            doc.rect(0, 0, pageWidth, pageHeight, 'F');
            doc.addImage(imgData, 'JPEG', 0, position, usableWidth, imgHeight);
            heightLeft -= pageHeight;
        }

        doc.save('Чек_ремонт_' + repairId + '.pdf');
        el.remove();
    }).catch(function(err) {
        console.error('PDF generation failed:', err);
        el.remove();
        alert('Не удалось сгенерировать PDF. Попробуйте ещё раз.');
    });
}

function generateRepairWarranty(repairId, data) {
    var existing = document.getElementById('repairWarrantyTemp');
    if (existing) existing.remove();

    var warrantyDate = new Date();
    warrantyDate.setFullYear(warrantyDate.getFullYear() + 1);
    var warrantyUntil = warrantyDate.toLocaleDateString('ru-RU');

    var partsList = '';
    if (data.parts && data.parts.length > 0) {
        data.parts.forEach(function(p) {
            partsList += '<li style="color:#b0b0b0; font-size:14px; margin-bottom:4px;">' + p.name + ' — ' + p.price.toFixed(2) + ' BYN</li>';
        });
    }

    var html = '<div id="repairWarrantyTemp" style="position:absolute; left:-9999px; top:0; width:800px; padding:50px; background:#1a1a1a; font-family:\'Montserrat\', sans-serif; color:#f0f0f0;">' +
        '<div style="text-align:center; margin-bottom:35px; padding-bottom:20px; border-bottom:2px solid #D44177;">' +
        '<h1 style="font-size:28px; color:#D44177; margin:0 0 5px 0; letter-spacing:2px;">RXXMRKT</h1>' +
        '<p style="font-size:13px; color:#888; margin:0 0 8px 0; letter-spacing:1px;">Ремонт смартфонов</p>' +
        '<p style="font-size:18px; color:#f0f0f0; margin:0; font-weight:700;">Гарантийный лист</p>' +
        '<p style="font-size:14px; color:#888; margin:5px 0 0 0;">№' + repairId + '</p>' +
        '</div>' +

        '<div style="padding:24px; background:#252525; border-radius:10px; margin-bottom:25px;">' +
        '<h3 style="font-size:16px; color:#D44177; margin:0 0 16px 0;">Информация об устройстве</h3>' +
        '<div style="font-size:14px;">' +
        '<div style="display:flex; margin-bottom:10px;"><span style="color:#888; min-width:160px; flex-shrink:0;">Устройство:</span><span style="color:#f0f0f0;">' + data.model + '</span></div>' +
        (data.serialNumber ? '<div style="display:flex; margin-bottom:10px;"><span style="color:#888; min-width:160px; flex-shrink:0;">Серийный номер:</span><span style="color:#f0f0f0;">' + data.serialNumber + '</span></div>' : '') +
        '<div style="display:flex; margin-bottom:10px;"><span style="color:#888; min-width:160px; flex-shrink:0;">Дата ремонта:</span><span style="color:#f0f0f0;">' + new Date().toLocaleDateString('ru-RU') + '</span></div>' +
        '<div style="display:flex;"><span style="color:#888; min-width:160px; flex-shrink:0;">Гарантия до:</span><span style="color:#D44177; font-weight:600;">' + warrantyUntil + '</span></div>' +
        '</div></div>';

    if (data.adminNotes || partsList) {
        html += '<div style="padding:24px; background:#252525; border-radius:10px; margin-bottom:25px;">' +
            '<h3 style="font-size:16px; color:#D44177; margin:0 0 16px 0;">Выполненные работы</h3>';

        if (data.adminNotes) {
            html += '<p style="color:#b0b0b0; font-size:14px; margin:0 0 12px 0;">' + data.adminNotes + '</p>';
        }

        if (partsList) {
            html += '<p style="color:#888; font-size:13px; margin:0 0 6px 0;">Использованные запчасти:</p>' +
                '<ul style="margin:0; padding-left:20px;">' + partsList + '</ul>';
        }

        html += '</div>';
    }

    html += '<div style="padding:24px; background:rgba(212,65,119,0.06); border:1px solid rgba(212,65,119,0.2); border-radius:10px; margin-bottom:25px;">' +
        '<h3 style="font-size:16px; color:#D44177; margin:0 0 16px 0;">Гарантийные обязательства</h3>' +
        '<p style="color:#b0b0b0; font-size:14px; line-height:1.6; margin:0 0 12px 0;">RXXMRKT предоставляет гарантию на выполненные работы и установленные запчасти сроком на <strong style="color:#f0f0f0;">12 месяцев</strong> с даты выполнения ремонта.</p>' +
        '<p style="color:#b0b0b0; font-size:14px; line-height:1.6; margin:0 0 12px 0;">Гарантия распространяется на устранение дефектов, возникших в результате некачественного выполнения работ или использования бракованных запчастей.</p>' +
        '<p style="color:#888; font-size:13px; line-height:1.6; margin:0;">Гарантия не распространяется на механические повреждения, попадание жидкости, программные сбои и повреждения, вызванные неправильной эксплуатацией.</p>' +
        '</div>';

    var masterName = data.masterName || '_________________';
    var clientName = data.clientName || '_________________';

    html += '<div style="padding:20px; background:#252525; border-radius:10px; margin-bottom:25px;">' +
        '<div style="display:flex; gap:20px;">' +
        '<div style="flex:1;">' +
        '<p style="color:#888; font-size:12px; margin:0 0 8px 0;">Подпись мастера</p>' +
        '<div style="border-top:1px solid #3a3a3a; padding-top:4px;"><span style="color:#666; font-size:12px;">_________________</span></div>' +
        '<p style="color:#b0b0b0; font-size:11px; margin:6px 0 0 0;">' + masterName + '</p>' +
        '</div>' +
        '<div style="flex:1;">' +
        '<p style="color:#888; font-size:12px; margin:0 0 8px 0;">Подпись клиента</p>' +
        '<div style="border-top:1px solid #3a3a3a; padding-top:4px;"><span style="color:#666; font-size:12px;">_________________</span></div>' +
        '<p style="color:#b0b0b0; font-size:11px; margin:6px 0 0 0;">' + clientName + '</p>' +
        '</div>' +
        '</div></div>';

    html += '<div style="text-align:center; margin-top:30px; padding-top:15px; border-top:1px solid #2c2c2c;">' +
        '<p style="font-size:14px; color:#D44177; margin:0 0 4px 0; font-weight:600;">RXXMRKT</p>' +
        '<p style="font-size:12px; color:#666; margin:0;">Гарантия на ремонт: 12 месяцев</p>' +
        '</div></div>';

    document.body.insertAdjacentHTML('beforeend', html);

    var el = document.getElementById('repairWarrantyTemp');

    html2canvas(el, {
        scale: 3,
        backgroundColor: '#1a1a1a',
        allowTaint: false,
        useCORS: true,
        logging: false
    }).then(function(canvas) {
        var imgData = canvas.toDataURL('image/jpeg', 0.97);
        var jsPDF = window.jspdf.jsPDF;
        var doc = new jsPDF('p', 'mm', 'a4');
        var pageWidth = doc.internal.pageSize.getWidth();
        var pageHeight = doc.internal.pageSize.getHeight();
        var usableWidth = pageWidth;
        var imgHeight = (canvas.height * usableWidth) / canvas.width;
        var heightLeft = imgHeight;
        var position = 0;

        doc.setFillColor(26, 26, 26);
        doc.rect(0, 0, pageWidth, pageHeight, 'F');
        doc.addImage(imgData, 'JPEG', 0, 0, usableWidth, imgHeight);
        heightLeft -= pageHeight;

        while (heightLeft > 0) {
            position = -(imgHeight - heightLeft);
            doc.addPage();
            doc.setFillColor(26, 26, 26);
            doc.rect(0, 0, pageWidth, pageHeight, 'F');
            doc.addImage(imgData, 'JPEG', 0, position, usableWidth, imgHeight);
            heightLeft -= pageHeight;
        }

        doc.save('Гарантия_ремонт_' + repairId + '.pdf');
        el.remove();
    }).catch(function(err) {
        console.error('PDF generation failed:', err);
        el.remove();
        alert('Не удалось сгенерировать PDF. Попробуйте ещё раз.');
    });
}
