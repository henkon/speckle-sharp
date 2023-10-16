using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.IO;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Revit.Async;
using Speckle.Core.Kits;
using Speckle.Core.Logging;


namespace Speckle.ConnectorRevit
{
  public static class ConnectorRevitUtils
  {
#if REVIT2023
    public static string RevitAppName = HostApplications.Revit.GetVersion(HostAppVersion.v2023);
#elif REVIT2022
    public static string RevitAppName = HostApplications.Revit.GetVersion(HostAppVersion.v2022);
#elif REVIT2021
    public static string RevitAppName = HostApplications.Revit.GetVersion(HostAppVersion.v2021);
#elif REVIT2020
    public static string RevitAppName = HostApplications.Revit.GetVersion(HostAppVersion.v2020);
#else
    public static string RevitAppName = HostApplications.Revit.GetVersion(HostAppVersion.v2019);
#endif


    private static string strGuidCommitId = "D913AEDC-D516-4452-BBC4-8CA65F796F46";
    private static string strGuidStreamId = "6683BAC9-04FD-4C22-B865-21505DF33979";
    private static string _sharedParamFilePath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\coBuilder";
    private static List<string> _cachedParameters = null;
    private static List<string> _cachedViews = null;
    public static List<SpeckleException> ConversionErrors { get; set; }

    private static Dictionary<string, Category> _categories { get; set; }

    public static Dictionary<string, Category> GetCategories(Document doc)
    {
      if (_categories == null)
      {
        _categories = new Dictionary<string, Category>();
        foreach (var bic in SupportedBuiltInCategories)
        {
          var category = Category.GetCategory(doc, bic);
          if (category == null)
            continue;
          //some categories, in other languages (eg DEU) have duplicated names #542
          if (_categories.ContainsKey(category.Name))
          {
            var spec = category.Id.ToString();
            if (category.Parent != null)
              spec = category.Parent.Name;
            _categories.Add(category.Name + " (" + spec + ")", category);
          }

          else
            _categories.Add(category.Name, category);
        }
      }

      return _categories;
    }


    //HK
    public static string GetShareParameterFileName(Document doc)
    {
      Autodesk.Revit.ApplicationServices.Application app = doc.Application;
      string a = app.SharedParametersFilename;
      return a;
    }

    public static string GetSharedParamFile(Document doc)
    {
      var path = Path.Combine(_sharedParamFilePath, GetShareParameterFileName(doc));
      return path;
    }

    private static Definition CreateDefinition(string paramName, ParameterType paramType, Guid strGuid, bool isVisible, DefinitionGroup defGroup)
    {
      ExternalDefinitionCreationOptions options = new ExternalDefinitionCreationOptions(paramName, paramType);
      options.Name = paramName;
      options.GUID = strGuid;
      options.Visible = isVisible;

      var definition = defGroup.Definitions.Create(options);
      return definition;
    }

    public static string GetSpeckleCommitId()
    {
      return strGuidCommitId;
    }

    public static string GetSpeckleStreamId()
    {
      return strGuidStreamId;
    }

    public static string GetSpeckleProjectParamValue(Document doc, string strGuid)
    {
      Parameter pp;
      //Autodesk.Revit.ApplicationServices.Application _app = doc.Application;

      Element projInfo = doc.ProjectInformation;
      Guid tt = Guid.Parse(strGuid);
      pp = projInfo.get_Parameter(tt);

      if (pp == null)
        return "";
      else
        return pp.AsString();
    }

    public static async Task<int> AddSpeckleSharedParameters(Document doc, string streamid, string commitid)
    {

      Autodesk.Revit.ApplicationServices.Application _app = doc.Application;
      
      Element projInfo = doc.ProjectInformation;
      int status = 0;
      //Parameter paramProjectNumber = _app.HelperParams.GetOrCreateElemSharedParameter(projInfo, ShareParamCoBuilderNumber, ParamGroup, ParameterType.Text, true, ShareParamCoBuilderNumberGuid);
      //BindSharedParamResult result = BindSharedParameter(elem.Document, elem.Category, paramName, grpName, paramType, instanceBinding, strGuid, isVisible);

      using (DefinitionFile defFile = _app.OpenSharedParameterFile())
      {
        string ParamGroup = "coBuilder";
        string paramName = "SpeckleCommitId";
        
        ParameterType paramType = ParameterType.Text;
        Guid strGuidCommit = Guid.Parse(strGuidCommitId);
        Guid strGuidStream = Guid.Parse(strGuidStreamId);
        bool isVisible = true;

        Parameter pm;
        //bool success;
        //Exception exception;

        

        var (success, exception) = await RevitTask.RunAsync(app =>
        {
          //string transactionName = $"Baking stream {state.StreamId}";
          //using var g = new TransactionGroup(CurrentDoc.Document, transactionName);
          using var t = new Transaction(doc, "CreateProjParam");

          //g.Start();
          //var failOpts = t.GetFailureHandlingOptions();
          //failOpts.SetFailuresPreprocessor(new ErrorEater(converter));
          //failOpts.SetClearAfterRollback(true);
          //t.SetFailureHandlingOptions(failOpts);
          t.Start();


          try
          {
            Category elementCategory = projInfo.Category;

            //HK need more testing whether paramgroup exists, etc...

            var defGroup = defFile.Groups.get_Item(ParamGroup);
            
            CategorySet categorySet = _app.Create.NewCategorySet();
            //using (Transaction tran = new Transaction(doc))
            //{
            //tran.Start("CreateProjParam");

            pm = projInfo.get_Parameter(strGuidCommit);
            if (pm == null)
            {
              Definition definition = defGroup.Definitions.get_Item(paramName);
              if (definition == null)
                definition = CreateDefinition(paramName, paramType, strGuidCommit, isVisible, defGroup); // HK: here created param

              categorySet.Insert(elementCategory);

              try
              {
                Binding newBinding = null;
                newBinding = _app.Create.NewInstanceBinding(categorySet);

                var inserted = doc.ParameterBindings.Insert(definition, newBinding);
                if (inserted)
                {
                  //return (1, "ok");
                  status = 1;
                }
              }
              catch (Exception ex) { }
             
              pm = projInfo.get_Parameter(strGuidCommit);
              pm.Set(commitid);
            }

            pm = projInfo.get_Parameter(strGuidStream);

            if (pm == null)
            {

              paramName = "SpeckleStreamId";
              Definition definition2 = defGroup.Definitions.get_Item(paramName);

              if (definition2 == null)
                definition2 = CreateDefinition(paramName, paramType, strGuidStream, isVisible, defGroup); // HK: here created param
                                                                                                        //CategorySet categorySet2 = _app.Create.NewCategorySet(); // do not need to create a new one?


              //categorySet.Insert(elementCategory);

              Binding newBinding2 = null;
              newBinding2 = _app.Create.NewInstanceBinding(categorySet);

              var inserted2 = doc.ParameterBindings.Insert(definition2, newBinding2);
              if (inserted2)
              {
                //return (1, "ok");
                status = 1;
              }
            
              pm = projInfo.get_Parameter(strGuidStream);
              pm.Set(streamid);
            }
            t.Commit();
            //}
          }
          catch (Exception ex)
          {
            
            return (0, "not ok");
          }
          return (1, "ok");
          //BuiltInCategory.OST_ProjectInformation
        }).ConfigureAwait(false);
      }
      return status;
    }
    /// <summary>
    /// We want to display a user-friendly category names when grouping objects
    /// For this we are simplifying the BuiltIn one as otherwise, by using the display value, we'd be getting localized category names
    /// which would make querying etc more difficult
    /// TODO: deprecate this in favour of model collections
    /// </summary>
    /// <param name="category"></param>
    /// <returns></returns>
    public static string GetEnglishCategoryName(Category category)
    {
      var builtInCategory = (BuiltInCategory)category.Id.IntegerValue;
      var builtInCategoryName = builtInCategory.ToString()
        .Replace("OST_IOS", "") //for OST_IOSModelGroups
        .Replace("OST_MEP", "") //for OST_MEPSpaces
        .Replace("OST_", "") //for any other OST_blablabla
        .Replace("_", " ");
      builtInCategoryName = Regex.Replace(builtInCategoryName, "([a-z])([A-Z])", "$1 $2", RegexOptions.Compiled).Trim();
      return builtInCategoryName;
    }

    #region extension methods

    public static List<Element> SupportedElements(this Document doc)
    {
      //get element types of supported categories
      var categoryFilter = new LogicalOrFilter(GetCategories(doc).Select(x => new ElementCategoryFilter(x.Value.Id))
        .Cast<ElementFilter>().ToList());

      List<Element> elements = new FilteredElementCollector(doc)
        .WhereElementIsNotElementType()
        .WhereElementIsViewIndependent()
        .WherePasses(categoryFilter).ToList();

      return elements;
    }

    public static List<Element> SupportedTypes(this Document doc)
    {
      //get element types of supported categories
      var categoryFilter = new LogicalOrFilter(GetCategories(doc).Select(x => new ElementCategoryFilter(x.Value.Id))
        .Cast<ElementFilter>().ToList());

      List<Element> elements = new FilteredElementCollector(doc)
        .WhereElementIsElementType()
        .WherePasses(categoryFilter).ToList();

      return elements;
    }

    public static List<View> Views2D(this Document doc)
    {
      List<View> views = new FilteredElementCollector(doc)
        .WhereElementIsNotElementType()
        .OfCategory(BuiltInCategory.OST_Views)
        .Cast<View>()
        .Where(x => x.ViewType == ViewType.CeilingPlan ||
                    x.ViewType == ViewType.FloorPlan ||
                    x.ViewType == ViewType.Elevation ||
                    x.ViewType == ViewType.Section)
        .ToList();

      return views;
    }

    public static List<View> Views3D(this Document doc)
    {
      List<View> views = new FilteredElementCollector(doc)
        .WhereElementIsNotElementType()
        .OfCategory(BuiltInCategory.OST_Views)
        .Cast<View>()
        .Where(x => x.ViewType == ViewType.ThreeD)
        .ToList();

      return views;
    }

    public static List<Element> Levels(this Document doc)
    {
      List<Element> levels = new FilteredElementCollector(doc)
        .WhereElementIsNotElementType()
        .OfCategory(BuiltInCategory.OST_Levels).ToList();

      return levels;
    }

    #endregion

    public static List<string> GetCategoryNames(Document doc)
    {
      return GetCategories(doc).Keys.OrderBy(x => x).ToList();
    }

    public static List<string> GetWorksets(Document doc)
    {
      return new FilteredWorksetCollector(doc).Where(x => x.Kind == WorksetKind.UserWorkset).Select(x => x.Name)
        .ToList();
    }

    private static async Task<List<string>> GetParameterNamesAsync(Document doc)
    {
      var els = new FilteredElementCollector(doc)
        .WhereElementIsNotElementType()
        .WhereElementIsViewIndependent()
        .Where(x => x.IsPhysicalElement());

      List<string> parameters = new List<string>();

      foreach (var e in els)
      {
        foreach (Parameter p in e.Parameters)
        {
          if (!parameters.Contains(p.Definition.Name))
            parameters.Add(p.Definition.Name);
        }
      }

      _cachedParameters = parameters.OrderBy(x => x).ToList();
      return _cachedParameters;
    }

    /// <summary>
    /// Each time it's called the cached parameters are returned, and a new copy is cached
    /// </summary>
    /// <param name="doc"></param>
    /// <returns></returns>
    public static List<string> GetParameterNames(Document doc)
    {
      if (_cachedParameters != null)
      {
        //don't wait for it to finish
        GetParameterNamesAsync(doc);
        return _cachedParameters;
      }

      return GetParameterNamesAsync(doc).Result;
    }

    private static async Task<List<string>> GetViewNamesAsync(Document doc)
    {
      var els = new FilteredElementCollector(doc)
        .WhereElementIsNotElementType()
        .OfClass(typeof(View))
        .ToElements();

      _cachedViews = els.Select(x => x.Name).OrderBy(x => x).ToList();
      return _cachedViews;
    }

    /// <summary>
    /// Each time it's called the cached parameters are return, and a new copy is cached
    /// </summary>
    /// <param name="doc"></param>
    /// <returns></returns>
    public static List<string> GetViewNames(Document doc)
    {
      if (_cachedViews != null)
      {
        //don't wait for it to finish
        GetViewNamesAsync(doc);
        return _cachedViews;
      }

      return GetViewNamesAsync(doc).Result;
    }

    public static bool IsPhysicalElement(this Element e)
    {
      if (e.Category == null) return false;
      if (e.ViewSpecific) return false;
      // exclude specific unwanted categories
      if (((BuiltInCategory)e.Category.Id.IntegerValue) == BuiltInCategory.OST_HVAC_Zones) return false;
      return e.Category.CategoryType == CategoryType.Model && e.Category.CanAddSubcategory;
    }

    public static bool IsElementSupported(this Element e)
    {
      if (e.Category == null) return false;
      if (e.ViewSpecific) return false;

      if (SupportedBuiltInCategories.Contains((BuiltInCategory)e.Category.Id.IntegerValue))
        return true;
      return false;
    }

    /// <summary>
    /// Removes all inherited classes from speckle type string
    /// </summary>
    /// <param name="s"></param>
    /// <returns></returns>
    public static string SimplifySpeckleType(string type)
    {
      return type.Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
    }

    public static string ObjectDescriptor(Element obj)
    {
      var simpleType = obj.GetType().ToString().Split(new string[] { "DB." }, StringSplitOptions.RemoveEmptyEntries)
        .LastOrDefault();
      return string.IsNullOrEmpty(obj.Name) ? $"{simpleType}" : $"{simpleType} {obj.Name}";
    }

    //list of currently supported Categories (for sending only)
    //exact copy of the one in the ConverterRevitShared.Categories
    //until issue https://github.com/specklesystems/speckle-sharp/issues/392 is resolved
    private static List<BuiltInCategory> SupportedBuiltInCategories = new List<BuiltInCategory>
    {
      BuiltInCategory.OST_Areas,
      BuiltInCategory.OST_CableTray,
      BuiltInCategory.OST_Ceilings,
      BuiltInCategory.OST_Columns,
      BuiltInCategory.OST_CommunicationDevices,
      BuiltInCategory.OST_Conduit,
      BuiltInCategory.OST_CurtaSystem,
      BuiltInCategory.OST_DataDevices,
      BuiltInCategory.OST_Doors,
      BuiltInCategory.OST_DuctSystem,
      BuiltInCategory.OST_DuctCurves,
      BuiltInCategory.OST_DuctFitting,
      BuiltInCategory.OST_DuctInsulations,
      BuiltInCategory.OST_ElectricalCircuit,
      BuiltInCategory.OST_ElectricalEquipment,
      BuiltInCategory.OST_ElectricalFixtures,
      BuiltInCategory.OST_Fascia,
      BuiltInCategory.OST_FireAlarmDevices,
      BuiltInCategory.OST_FlexDuctCurves,
      BuiltInCategory.OST_FlexPipeCurves,
      BuiltInCategory.OST_Floors,
      BuiltInCategory.OST_GenericModel,
      BuiltInCategory.OST_Grids,
      BuiltInCategory.OST_Gutter,
      //BuiltInCategory.OST_HVAC_Zones,
      BuiltInCategory.OST_IOSModelGroups,
      BuiltInCategory.OST_LightingDevices,
      BuiltInCategory.OST_LightingFixtures,
      BuiltInCategory.OST_Lines,
      BuiltInCategory.OST_Mass,
      BuiltInCategory.OST_MassFloor,
      BuiltInCategory.OST_Materials,
      BuiltInCategory.OST_MechanicalEquipment,
      BuiltInCategory.OST_MEPSpaces,
      BuiltInCategory.OST_Parking,
      BuiltInCategory.OST_PipeCurves,
      BuiltInCategory.OST_PipingSystem,
      BuiltInCategory.OST_PointClouds,
      BuiltInCategory.OST_PointLoads,
      BuiltInCategory.OST_StairsRailing,
      BuiltInCategory.OST_RailingSupport,
      BuiltInCategory.OST_RailingTermination,
      BuiltInCategory.OST_Rebar,
      BuiltInCategory.OST_Roads,
      BuiltInCategory.OST_RoofSoffit,
      BuiltInCategory.OST_Roofs,
      BuiltInCategory.OST_Rooms,
      BuiltInCategory.OST_SecurityDevices,
      BuiltInCategory.OST_ShaftOpening,
      BuiltInCategory.OST_Site,
      BuiltInCategory.OST_EdgeSlab,
      BuiltInCategory.OST_Stairs,
      BuiltInCategory.OST_AreaRein,
      BuiltInCategory.OST_StructuralFramingSystem,
      BuiltInCategory.OST_StructuralColumns,
      BuiltInCategory.OST_StructConnections,
      BuiltInCategory.OST_FabricAreas,
      BuiltInCategory.OST_FabricReinforcement,
      BuiltInCategory.OST_StructuralFoundation,
      BuiltInCategory.OST_StructuralFraming,
      BuiltInCategory.OST_PathRein,
      BuiltInCategory.OST_StructuralStiffener,
      BuiltInCategory.OST_StructuralTruss,
      BuiltInCategory.OST_SwitchSystem,
      BuiltInCategory.OST_TelephoneDevices,
      BuiltInCategory.OST_Topography,
      BuiltInCategory.OST_Cornices,
      BuiltInCategory.OST_Walls,
      BuiltInCategory.OST_Windows,
      BuiltInCategory.OST_Wire,
      BuiltInCategory.OST_Casework,
      BuiltInCategory.OST_CurtainWallPanels,
      BuiltInCategory.OST_CurtainWallMullions,
      BuiltInCategory.OST_Entourage,
      BuiltInCategory.OST_Furniture,
      BuiltInCategory.OST_FurnitureSystems,
      BuiltInCategory.OST_Planting,
      BuiltInCategory.OST_PlumbingFixtures,
      BuiltInCategory.OST_Ramps,
      BuiltInCategory.OST_SpecialityEquipment,
      BuiltInCategory.OST_Rebar,
#if (REVIT2020 || REVIT2021)

#else
      BuiltInCategory.OST_AudioVisualDevices,
      BuiltInCategory.OST_FireProtection,
      BuiltInCategory.OST_FoodServiceEquipment,
      BuiltInCategory.OST_Hardscape,
      BuiltInCategory.OST_MedicalEquipment,
      BuiltInCategory.OST_Signage,
      BuiltInCategory.OST_TemporaryStructure,
      BuiltInCategory.OST_VerticalCirculation,
#endif
#if REVIT2020 || REVIT2021 || REVIT2022
#else
      BuiltInCategory.OST_MechanicalControlDevices,
#endif
    };
  }
}
