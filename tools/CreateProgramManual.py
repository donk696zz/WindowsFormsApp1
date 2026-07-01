from pathlib import Path
from zipfile import ZipFile, ZIP_DEFLATED
from html import escape
import struct
import sys


W = "http://schemas.openxmlformats.org/wordprocessingml/2006/main"
R = "http://schemas.openxmlformats.org/officeDocument/2006/relationships"
WP = "http://schemas.openxmlformats.org/drawingml/2006/wordprocessingDrawing"
A = "http://schemas.openxmlformats.org/drawingml/2006/main"
PIC = "http://schemas.openxmlformats.org/drawingml/2006/picture"


def run(text, bold=False, size=None):
    props = []
    if bold:
        props.append("<w:b/>")
    if size:
        props.append(f'<w:sz w:val="{size}"/><w:szCs w:val="{size}"/>')
    rpr = f"<w:rPr>{''.join(props)}</w:rPr>" if props else ""
    return f'<w:r>{rpr}<w:t xml:space="preserve">{escape(text)}</w:t></w:r>'


def paragraph(text="", style=None, bold=False, size=None, align=None, after=100):
    props = []
    if style:
        props.append(f'<w:pStyle w:val="{style}"/>')
    if align:
        props.append(f'<w:jc w:val="{align}"/>')
    props.append(f'<w:spacing w:after="{after}" w:line="360" w:lineRule="auto"/>')
    return f'<w:p><w:pPr>{"".join(props)}</w:pPr>{run(text, bold, size)}</w:p>'


def page_break():
    return '<w:p><w:r><w:br w:type="page"/></w:r></w:p>'


def table(headers, rows, widths):
    grid = "".join(f'<w:gridCol w:w="{x}"/>' for x in widths)
    result = [
        '<w:tbl><w:tblPr><w:tblW w:w="0" w:type="auto"/>',
        '<w:tblBorders><w:top w:val="single" w:sz="4" w:color="B7C9DC"/>',
        '<w:left w:val="single" w:sz="4" w:color="B7C9DC"/>',
        '<w:bottom w:val="single" w:sz="4" w:color="B7C9DC"/>',
        '<w:right w:val="single" w:sz="4" w:color="B7C9DC"/>',
        '<w:insideH w:val="single" w:sz="4" w:color="D9E2F3"/>',
        '<w:insideV w:val="single" w:sz="4" w:color="D9E2F3"/></w:tblBorders>',
        '<w:tblCellMar><w:top w:w="100" w:type="dxa"/><w:left w:w="120" w:type="dxa"/>',
        '<w:bottom w:w="100" w:type="dxa"/><w:right w:w="120" w:type="dxa"/></w:tblCellMar>',
        f'</w:tblPr><w:tblGrid>{grid}</w:tblGrid>'
    ]

    def row(values, header=False):
        cells = []
        for value, width in zip(values, widths):
            shade = '<w:shd w:fill="D9EAF7"/>' if header else ""
            cells.append(
                f'<w:tc><w:tcPr><w:tcW w:w="{width}" w:type="dxa"/>{shade}</w:tcPr>'
                f'{paragraph(str(value), bold=header, after=0)}</w:tc>')
        return f'<w:tr>{"".join(cells)}</w:tr>'

    result.append(row(headers, True))
    result.extend(row(values) for values in rows)
    result.append('</w:tbl>')
    result.append(paragraph("", after=80))
    return "".join(result)


def png_size(path):
    data = path.read_bytes()[:24]
    return struct.unpack(">II", data[16:24])


def image_paragraph(rel_id, path, doc_pr_id, caption):
    width, height = png_size(path)
    max_w = 6.35
    max_h = 7.2
    scale = min(max_w / width, max_h / height)
    cx = int(width * scale * 914400)
    cy = int(height * scale * 914400)
    drawing = f'''
<w:p><w:pPr><w:jc w:val="center"/><w:spacing w:after="80"/></w:pPr><w:r><w:drawing>
<wp:inline distT="0" distB="0" distL="0" distR="0">
<wp:extent cx="{cx}" cy="{cy}"/><wp:effectExtent l="0" t="0" r="0" b="0"/>
<wp:docPr id="{doc_pr_id}" name="{escape(caption)}"/><wp:cNvGraphicFramePr>
<a:graphicFrameLocks xmlns:a="{A}" noChangeAspect="1"/></wp:cNvGraphicFramePr>
<a:graphic xmlns:a="{A}"><a:graphicData uri="http://schemas.openxmlformats.org/drawingml/2006/picture">
<pic:pic xmlns:pic="{PIC}"><pic:nvPicPr><pic:cNvPr id="0" name="{escape(path.name)}"/>
<pic:cNvPicPr/></pic:nvPicPr><pic:blipFill><a:blip r:embed="{rel_id}"/>
<a:stretch><a:fillRect/></a:stretch></pic:blipFill><pic:spPr><a:xfrm><a:off x="0" y="0"/>
<a:ext cx="{cx}" cy="{cy}"/></a:xfrm><a:prstGeom prst="rect"><a:avLst/></a:prstGeom>
<a:ln><a:solidFill><a:srgbClr val="A6A6A6"/></a:solidFill></a:ln></pic:spPr></pic:pic>
</a:graphicData></a:graphic></wp:inline></w:drawing></w:r></w:p>'''
    return drawing + paragraph(caption, style="Caption", align="center")


def build_document(assets):
    body = []
    body.append(paragraph("四轴视觉检测程序", style="Title", align="center", after=160))
    body.append(paragraph("页面操作说明书", style="Subtitle", align="center", after=500))
    body.append(paragraph("适用页面：料号管理、料号设置、参数界面、调试界面", align="center"))
    body.append(paragraph("说明：本手册重点说明页面功能、按钮作用和标准操作方法。", align="center"))
    body.append(page_break())

    body.append(paragraph("1  料号管理页面", style="Heading1"))
    body.append(paragraph("1.1 页面功能", style="Heading2"))
    body.append(paragraph("料号管理页面用于建立产品料号、选择当前生产料号、删除无用料号以及刷新料号列表。不同料号可以在料号设置页面保存各自的定位框参数。"))
    body.append(image_paragraph("rId10", assets[0], 10, "图1  料号管理页面"))
    body.append(paragraph("1.2 控件和按钮说明", style="Heading2"))
    body.append(table(
        ["控件/按钮", "点击或操作后执行的功能"],
        [
            ("左侧料号列表", "显示已有料号。单击某个料号后，该名称会显示到右侧输入框。"),
            ("料号输入框", "输入准备新建的料号名称。名称不能为空、不能重复，也不能包含文件名非法字符。"),
            ("新建料号", "校验名称并创建料号；创建成功后，新料号自动成为当前生产料号。"),
            ("设为当前料号", "把左侧选中的料号切换为当前生产料号，并加载该料号对应的区域配置。"),
            ("删除料号", "确认后删除选中料号。若删除当前料号，程序自动切换到剩余料号；没有剩余料号时自动建立“默认料号”。"),
            ("刷新", "重新读取料号文件并刷新左侧列表。"),
            ("状态提示", "显示创建、切换、删除或刷新是否成功。")
        ], [2400, 6500]))
    body.append(paragraph("1.3 使用方法", style="Heading2"))
    body.append(paragraph("新建：在输入框填写新料号 → 点击“新建料号” → 查看状态提示和当前生产料号。"))
    body.append(paragraph("切换：在左侧选择目标料号 → 点击“设为当前料号” → 确认顶部当前生产料号已变化。"))
    body.append(paragraph("删除：在左侧选择料号 → 点击“删除料号” → 在确认窗口选择“是”。"))
    body.append(paragraph("注意：左侧只选中料号不会切换生产料号，必须点击“设为当前料号”。", bold=True))
    body.append(page_break())

    body.append(paragraph("2  料号设置页面", style="Heading1"))
    body.append(paragraph("2.1 页面功能", style="Heading2"))
    body.append(paragraph("料号设置页面用于给指定料号配置产品定位范围、左右银面区域、中间区域和边缘检测区域。页面中的参数按料号分别保存。"))
    body.append(image_paragraph("rId11", assets[1], 11, "图2  料号设置页面"))
    body.append(paragraph("2.2 顶部按钮说明", style="Heading2"))
    body.append(table(
        ["控件/按钮", "点击或操作后执行的功能"],
        [
            ("料号下拉框", "选择需要编辑配置的料号。只改变编辑对象，不改变当前生产料号。"),
            ("选择检测图", "打开图片选择窗口；载入图片后立即定位产品并显示检测区域框。"),
            ("保存检测图", "保存当前料号的框参数，同时保存当前带框检测图作为该料号参考图。"),
            ("恢复默认框", "把当前编辑参数恢复为默认值；只有再次点击“保存检测图”才正式保存。"),
            ("状态栏", "显示当前料号、产品框坐标、加载失败或保存结果。")
        ], [2400, 6500]))
    body.append(paragraph("2.3 参数区域说明", style="Heading2"))
    body.append(table(
        ["选项卡", "主要作用"],
        [
            ("框比例", "调整银面外边界、银面内边界、中间区域宽度、上下缩进、上下分割及边缘检查范围。"),
            ("定位高级", "调整产品暗像素阈值、行列有效比例、平滑半径和支持点，用于产品自动定位不稳定时修正。")
        ], [2400, 6500]))
    body.append(paragraph("2.4 标准操作", style="Heading2"))
    body.append(paragraph("1. 选择要编辑的料号。"))
    body.append(paragraph("2. 点击“选择检测图”，选择一张产品完整、清晰且位置具有代表性的图片。"))
    body.append(paragraph("3. 查看左侧框线是否正确覆盖产品、左右银面和中间区域。"))
    body.append(paragraph("4. 修改右侧参数；框线会随参数变化实时刷新。"))
    body.append(paragraph("5. 确认后点击“保存检测图”。"))
    body.append(paragraph("建议：先调整产品自动定位，再调整银面和中间区域比例；每次只小幅修改一个参数。", bold=True))
    body.append(paragraph("2.5 定位效果示例", style="Heading2"))
    body.append(paragraph("下图为从验证图片文件夹选取的实际产品定位效果。蓝色框表示产品主体，绿色框表示左右银面和中间区域，橙色框表示边缘检查区域。实际调试时应确认框线没有明显偏离产品。"))
    body.append(image_paragraph("rId14", assets[4], 14, "图3  产品及检测区域定位效果"))
    body.append(page_break())

    body.append(paragraph("3  参数界面", style="Heading1"))
    body.append(paragraph("3.1 页面功能", style="Heading2"))
    body.append(paragraph("参数界面用于设置所有料号共用的检测判定阈值、图像处理参数、调试结果保存路径和系统维护参数。进入此页面需要先完成登录。"))
    body.append(image_paragraph("rId12", assets[2], 12, "图4  参数界面"))
    body.append(paragraph("3.2 顶部按钮说明", style="Heading2"))
    body.append(table(
        ["控件/按钮", "点击或操作后执行的功能"],
        [
            ("保存参数", "校验当前数值并保存到系统参数文件，保存后正式用于检测。不会覆盖相机设置页面中的曝光和增益。"),
            ("恢复默认", "恢复检测参数默认值；相机曝光、增益及已设置的保存路径保持不变。需点击“保存参数”后生效。"),
            ("实时预览当前调试图", "勾选后，修改检测参数会自动重新处理调试页面当前图片，但修改仍处于未保存状态。"),
            ("状态栏", "提示预览是否成功、参数是否保存以及调试页面是否已经加载图片。")
        ], [2400, 6500]))
    body.append(paragraph("3.3 各选项卡说明", style="Heading2"))
    body.append(table(
        ["选项卡", "功能"],
        [
            ("OK/复检/NG判定", "设置银面上下半区覆盖率、内部缺陷面积、边缘凹陷有效面积和中间框残银阈值。"),
            ("图像处理高级", "设置银面掩码形态学、中间框灰度阈值及物料边界检查参数。"),
            ("调试结果存储", "设置调试检测完成后OK图片和NG/复检图片的保存目录；路径框显示当前目录。"),
            ("系统", "设置管理员权限超时时间和图片保留天数。")
        ], [2400, 6500]))
    body.append(paragraph("3.4 主要判定参数", style="Heading2"))
    body.append(table(
        ["参数", "作用及调整方向"],
        [
            ("银面覆盖率NG/OK", "左右银面分别按上、下半区判断。低于NG线判NG，高于OK线判OK，中间范围进入复检。"),
            ("内部缺陷复查/NG面积", "开运算后单个内部缺陷的像素面积。阈值越小越严格。"),
            ("边缘凹陷复查/NG面积", "与动态计算后的有效面积比较。阈值越小越严格。"),
            ("面积形状换算系数", "放大边缘缺陷有效面积。数值越大越容易进入复检或NG。"),
            ("最小平均厚度", "低于该厚度的细长候选被忽略。数值越大，细线过滤越强。"),
            ("中间框高亮占比", "中间区域高亮像素比例达到复检线或NG线时产生对应结果。")
        ], [2700, 6200]))
    body.append(paragraph("边缘凹陷计算公式", style="Heading3"))
    body.append(paragraph("有效面积 = 实际面积 × 最大深度 ÷ 开口宽度 × 换算系数", bold=True, align="center"))
    body.append(paragraph("开口越宽或深度越浅，需要更大的实际缺失面积才能判NG；平均厚度过小的细长区域会被忽略。"))
    body.append(paragraph("3.5 图像处理效果示例", style="Heading2"))
    body.append(paragraph("梯度掩码用于区分银面主体和暗缺陷：白色为高置信银面，灰色为亮度过渡区域，黑色为非银面或明显暗区。右侧银面中的黑色孔洞会进入后续内部缺陷筛选。"))
    body.append(image_paragraph("rId15", assets[5], 15, "图5  银面梯度掩码效果"))
    body.append(paragraph("边缘缺口分类图只保留经过圆角拟合和形状筛选后的候选。红色表示NG，绿色表示复检，黄色表示未达到复检阈值。下图两处红色区域对应实际边缘缺口。"))
    body.append(image_paragraph("rId16", assets[6], 16, "图6  边缘缺口分类效果"))
    body.append(paragraph("3.6 推荐调参方式", style="Heading2"))
    body.append(paragraph("1. 先在调试页面加载一组有代表性的OK、复检和NG图片。"))
    body.append(paragraph("2. 保持实时预览开启，每次只修改一类参数。"))
    body.append(paragraph("3. 使用“重新检测当前图”验证结果。"))
    body.append(paragraph("4. 同时检查多张OK图和NG图，确认没有明显误判后再点击“保存参数”。"))
    body.append(page_break())

    body.append(paragraph("4  调试界面", style="Heading1"))
    body.append(paragraph("4.1 页面功能", style="Heading2"))
    body.append(paragraph("调试界面用于对文件夹中的图片进行逐张或连续检测，显示检测框、缺陷位置和最终OK/复检/NG结果。"))
    body.append(image_paragraph("rId13", assets[3], 13, "图7  调试界面"))
    body.append(paragraph("4.2 控件和按钮说明", style="Heading2"))
    body.append(table(
        ["控件/按钮", "点击或操作后执行的功能"],
        [
            ("结果显示块", "显示NA、OK、复检或NG。绿色代表OK，橙色代表复检，红色代表NG。"),
            ("相机选择", "选择检测相机或分类相机；当前文件夹图片调试不依赖该选项。"),
            ("图片检测", "打开文件夹选择窗口，读取文件夹中的BMP、JPG、PNG、TIF等图片并开始检测。"),
            ("下一张", "自动检测未勾选时，检测列表中的下一张图片；全部完成后按钮不可用。"),
            ("重新检测当前图", "使用当前已保存参数重新检测正在显示的图片。"),
            ("自动检测", "勾选后，选择文件夹即连续检测全部图片；处理中会持续更新画面和结果。"),
            ("相机检测", "预留按钮；当前版本进行文件图片调试请使用“图片检测”。"),
            ("参数预览（未保存）", "参数界面进行实时预览时显示，表示画面使用的是尚未保存的参数。")
        ], [2400, 6500]))
    body.append(paragraph("4.3 逐张检测", style="Heading2"))
    body.append(paragraph("1. 取消勾选“自动检测”。"))
    body.append(paragraph("2. 点击“图片检测”，选择待检测图片文件夹。"))
    body.append(paragraph("3. 查看第一张图片的框线、缺陷标记和结果。"))
    body.append(paragraph("4. 点击“下一张”继续；需要复测时点击“重新检测当前图”。"))
    body.append(paragraph("4.4 自动检测", style="Heading2"))
    body.append(paragraph("1. 勾选“自动检测”。"))
    body.append(paragraph("2. 点击“图片检测”并选择文件夹。"))
    body.append(paragraph("3. 程序自动处理全部图片，完成后日志区域提示检测完成。"))
    body.append(paragraph("4.5 结果图片保存", style="Heading2"))
    body.append(paragraph("OK结果图保存到参数界面的“OK结果图路径”；NG和复检结果图保存到“NG/复检结果图路径”。同名文件自动增加序号，不会覆盖已有图片。"))

    body.append(paragraph("4.6 实际检测效果示例", style="Heading2"))
    body.append(paragraph("下图为验证图片的实际NG显示效果。程序在原图上保留检测区域框，并用红色框和文字标出达到NG条件的缺陷，便于人工核对误判原因。"))
    body.append(image_paragraph("rId17", assets[7], 17, "图8  实际NG缺陷标注效果"))
    body.append(paragraph("内部缺陷最终掩码中，黑色为无缺陷区域，白色为经过形态学处理和面积筛选后保留的内部暗缺陷。该图主要用于算法调试，正常操作时以调试页面的标注结果为准。"))
    body.append(image_paragraph("rId18", assets[8], 18, "图9  最终内部缺陷掩码"))

    body.append(paragraph("操作建议", style="Heading2"))
    body.append(paragraph("调参时优先使用逐张检测，确认参数稳定后再使用自动检测批量验证。出现异常结果时，应同时检查料号设置中的区域框和参数界面的判定阈值。"))

    body.append('<w:sectPr><w:pgSz w:w="11906" w:h="16838"/><w:pgMar w:top="1080" w:right="1080" w:bottom="1080" w:left="1080" w:header="720" w:footer="720"/></w:sectPr>')
    return "".join(body)


def create_docx(output, asset_dir):
    assets = [
        asset_dir / "01_material_management.png",
        asset_dir / "02_material_settings.png",
        asset_dir / "03_parameters.png",
        asset_dir / "04_debug.png",
        asset_dir / "05_roi_detection_effect.png",
        asset_dir / "06_gradient_mask_effect.png",
        asset_dir / "07_edge_classification_effect.png",
        asset_dir / "08_ng_detection_effect.png",
        asset_dir / "09_internal_defect_effect.png",
    ]
    for path in assets:
        if not path.exists():
            raise FileNotFoundError(path)

    document = f'''<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<w:document xmlns:w="{W}" xmlns:r="{R}" xmlns:wp="{WP}" xmlns:a="{A}" xmlns:pic="{PIC}">
<w:body>{build_document(assets)}</w:body></w:document>'''
    styles = f'''<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<w:styles xmlns:w="{W}">
<w:docDefaults><w:rPrDefault><w:rPr><w:rFonts w:ascii="Microsoft YaHei" w:eastAsia="Microsoft YaHei" w:hAnsi="Microsoft YaHei"/><w:sz w:val="21"/><w:szCs w:val="21"/></w:rPr></w:rPrDefault></w:docDefaults>
<w:style w:type="paragraph" w:default="1" w:styleId="Normal"><w:name w:val="Normal"/></w:style>
<w:style w:type="paragraph" w:styleId="Title"><w:name w:val="Title"/><w:basedOn w:val="Normal"/><w:rPr><w:b/><w:color w:val="1F4E79"/><w:sz w:val="44"/></w:rPr></w:style>
<w:style w:type="paragraph" w:styleId="Subtitle"><w:name w:val="Subtitle"/><w:basedOn w:val="Normal"/><w:rPr><w:color w:val="5B9BD5"/><w:sz w:val="30"/></w:rPr></w:style>
<w:style w:type="paragraph" w:styleId="Heading1"><w:name w:val="heading 1"/><w:basedOn w:val="Normal"/><w:keepNext/><w:rPr><w:b/><w:color w:val="1F4E79"/><w:sz w:val="32"/></w:rPr></w:style>
<w:style w:type="paragraph" w:styleId="Heading2"><w:name w:val="heading 2"/><w:basedOn w:val="Normal"/><w:keepNext/><w:rPr><w:b/><w:color w:val="2F75B5"/><w:sz w:val="26"/></w:rPr></w:style>
<w:style w:type="paragraph" w:styleId="Heading3"><w:name w:val="heading 3"/><w:basedOn w:val="Normal"/><w:keepNext/><w:rPr><w:b/><w:sz w:val="23"/></w:rPr></w:style>
<w:style w:type="paragraph" w:styleId="Caption"><w:name w:val="caption"/><w:basedOn w:val="Normal"/><w:rPr><w:i/><w:color w:val="666666"/><w:sz w:val="18"/></w:rPr></w:style>
</w:styles>'''
    rels = ['<Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles" Target="styles.xml"/>']
    for i, asset in enumerate(assets, 10):
        rels.append(f'<Relationship Id="rId{i}" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/image" Target="media/{asset.name}"/>')
    doc_rels = f'''<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">{"".join(rels)}</Relationships>'''
    package_rels = '''<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
<Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="word/document.xml"/>
</Relationships>'''
    content_types = '''<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types">
<Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/>
<Default Extension="xml" ContentType="application/xml"/>
<Default Extension="png" ContentType="image/png"/>
<Override PartName="/word/document.xml" ContentType="application/vnd.openxmlformats-officedocument.wordprocessingml.document.main+xml"/>
<Override PartName="/word/styles.xml" ContentType="application/vnd.openxmlformats-officedocument.wordprocessingml.styles+xml"/>
</Types>'''

    output.parent.mkdir(parents=True, exist_ok=True)
    with ZipFile(output, "w", ZIP_DEFLATED) as package:
        package.writestr("[Content_Types].xml", content_types)
        package.writestr("_rels/.rels", package_rels)
        package.writestr("word/document.xml", document)
        package.writestr("word/styles.xml", styles)
        package.writestr("word/_rels/document.xml.rels", doc_rels)
        for asset in assets:
            package.write(asset, f"word/media/{asset.name}")


if __name__ == "__main__":
    if len(sys.argv) != 3:
        raise SystemExit("usage: CreateProgramManual.py OUTPUT.docx ASSET_DIR")
    create_docx(Path(sys.argv[1]).resolve(), Path(sys.argv[2]).resolve())
    print(Path(sys.argv[1]).resolve())
