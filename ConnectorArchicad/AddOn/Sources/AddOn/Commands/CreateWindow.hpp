#ifndef CREATE_WINDOW_HPP
#define CREATE_WINDOW_HPP

#include "CreateOpeningBase.hpp"


namespace AddOnCommands {


class CreateWindow : public CreateOpeningBase {
	GS::UniString		GetUndoableCommandName () const override;
	GSErrCode			GetElementFromObjectState (const GS::ObjectState& os,
							API_Element& element,
							API_Element& elementMask,
							API_ElementMemo& memo,
							GS::UInt64& memoMask,
							AttributeManager& attributeManager,
							LibpartImportManager& libpartImportManager,
							API_SubElement** marker = nullptr) const override;

public:
	virtual GS::String	GetName () const override;
};


}


#endif
