#ifndef CREATE_COMMAND_HPP
#define CREATE_COMMAND_HPP

#include "APIEnvir.h"
#include "ACAPinc.h"
#include "LibpartImportManager.hpp"

#include "BaseCommand.hpp"

namespace AddOnCommands {


class CreateCommand : public BaseCommand {
	virtual GS::String		GetFieldName () const = 0;
	virtual GS::UniString	GetUndoableCommandName () const = 0;

	virtual GSErrCode		CreateNewElement (API_Element& element,
								API_ElementMemo& elementMemo,
								API_SubElement* marker = nullptr) const;

	virtual GSErrCode		ModifyExistingElement (API_Element& element,
								API_Element& elementMask,
								API_ElementMemo& memo,
								GS::UInt64 memoMask) const;

	virtual GSErrCode		GetElementFromObjectState (const GS::ObjectState& os,
								API_Element& element,
								API_Element& elementMask,
								API_ElementMemo& memo,
								GS::UInt64& memoMask,
								AttributeManager& attributeManager,
								LibpartImportManager& libpartImportManager,
								API_SubElement** marker = nullptr) const = 0;

public:
	virtual GS::ObjectState	Execute (const GS::ObjectState& parameters, GS::ProcessControl& processControl) const override;
};
}

#endif
