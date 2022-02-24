using CSiAPIv1;
using Objects.Structural.Geometry;
using Objects.Structural.Results;
using Speckle.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Objects.Converter.SAP2000
{
  public partial class ConverterSAP2000
  {

    public ResultSetAll ResultsToSpeckle()
    {
      #region Retrieve frame names
      int numberOfFrameNames = 0;
      var frameNames = new string[] { };

      Model.FrameObj.GetNameList(ref numberOfFrameNames, ref frameNames);
      frameNames.ToList();
      List<string> convertedFrameNames = frameNames.ToList();
      #endregion

      

      #region Retrieve area names

      int numberOfAreaNames = 0;
      var areaNames = new string[] { };

      Model.AreaObj.GetNameList(ref numberOfAreaNames, ref areaNames);

      List<string> convertedAreaNames = areaNames.ToList();

      #endregion

      ResultSetAll results = new ResultSetAll(AllResultSet1dToSpeckle(convertedFrameNames), AreaResultSet2dToSpeckle(convertedAreaNames), new ResultSet3D(), new ResultGlobal(), AllResultSetNodesToSpeckle());

      return results;
    }
  }

}