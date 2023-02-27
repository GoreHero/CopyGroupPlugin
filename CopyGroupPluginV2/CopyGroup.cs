using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection; //выбор объектов
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace CopyGroupPluginV2
{

    [TransactionAttribute(TransactionMode.Manual)]
    public class CopyGroup : IExternalCommand
    {
        //метод Execute должен возращать значение типа резалт, КОТОРЫЙ ПОКАЗЫВАЕТ УСПЕШНО ИЛИ НЕ Успешно завершилась команда
        //если не  успешно то все транзакции откатываются назад
        //метод принимает три аргумента
        //commandData с помощью него мы добераемся до информации в открытом документе ревит
        //message -это параметр передается по ссылке. если возращаемый результат неудача то вернется сообщение о причине
        //elements - если закончится неудачей то будет подсвечен элемент
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                //добираемся до документа
                //Application сама программа
                //UIApplication пользовательский интерфейс программы (например добавление кнопок)
                //UIDocument открытый файл в программе
                //Document - база данных открытого документа
                UIDocument uiDoc = commandData.Application.ActiveUIDocument;
                Document doc = uiDoc.Document;  //получили доступ к документу

                GroupPickFilter groupPickFilter = new GroupPickFilter();
                // ссылка на выбраную пользователем группу объектов, нельзя менять ссылку
                Reference reference = uiDoc.Selection.PickObject(ObjectType.Element, groupPickFilter, "Выберите группу объектов");
                //Elemtnt является родительстк=им классом для всех элементов( как object  в c#)
                Element element = doc.GetElement(reference);
                //Group group = (Group)element; - такая запись дает исключение 
                Group group = element as Group;

                XYZ groupCenter = GetElementCenter(group);
                Room room = GetRoomByPoint(doc, groupCenter);
                XYZ roomCenter = GetElementCenter(room);
                XYZ offset = groupCenter - roomCenter;

                //Запрашиваем точку
                XYZ point = uiDoc.Selection.PickPoint("Выберите точку");
                Room selectedRoom = GetRoomByPoint(doc, point);
                XYZ selectedRoomCenter = GetElementCenter(selectedRoom);
                XYZ selectedPoint = selectedRoomCenter + offset;

                Transaction transaction = new Transaction(doc);
                transaction.Start("Копирование группы объектов");
                doc.Create.PlaceGroup(selectedPoint, group.GroupType);
                transaction.Commit();
            }
            catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            {
                return Result.Cancelled;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return Result.Failed;
            }

            return Result.Succeeded;
        }

        public XYZ GetElementCenter(Element element)
        {
            BoundingBoxXYZ bounding = element.get_BoundingBox(null);
            return (bounding.Max + bounding.Min) / 2;
        }

        public Room GetRoomByPoint(Document doc, XYZ point)
        {
            FilteredElementCollector collector = new FilteredElementCollector(doc);
            collector.OfCategory(BuiltInCategory.OST_Rooms);
            foreach (Element e in collector)
            {
                Room room = e as Room;
                if (room != null)
                {
                    if (room.IsPointInRoom(point))
                    {
                        return room;
                    }
                }

            }
            return null;
        }
    }

    public class GroupPickFilter : ISelectionFilter
    {
        public bool AllowElement(Element elem)
        {
            if (elem.Category.Id.IntegerValue == (int)BuiltInCategory.OST_IOSModelGroups)
                return true;
            else
                return false;
        }

        public bool AllowReference(Reference reference, XYZ position)
        {
            return false;
        }
    }
}