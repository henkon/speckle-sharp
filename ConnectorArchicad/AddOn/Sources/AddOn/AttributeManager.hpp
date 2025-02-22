#ifndef ATTRIBUTE_MANAGER_HPP
#define ATTRIBUTE_MANAGER_HPP

#include "ModelInfo.hpp"

class AttributeManager {
private:
	static AttributeManager* instance;

	GS::HashTable< GS::UniString, API_Attribute> cache;

protected:
	AttributeManager ();

public:
	AttributeManager (AttributeManager&) = delete;
	void		operator=(const AttributeManager&) = delete;
	static AttributeManager*	GetInstance ();
	static void					DeleteInstance ();

	GSErrCode	GetMaterial (const ModelInfo::Material& material, API_Attribute& attribute);
	GSErrCode	GetDefaultMaterial (API_Attribute& attribute, GS::UniString& name);
};

#endif
